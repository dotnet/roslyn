// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateType
{
    internal abstract partial class AbstractGenerateTypeService<TService, TSimpleNameSyntax, TObjectCreationExpressionSyntax, TExpressionSyntax, TTypeDeclarationSyntax, TArgumentSyntax>
    {
        internal abstract IMethodSymbol GetDelegatingConstructor(TObjectCreationExpressionSyntax objectCreation, INamedTypeSymbol namedType, SemanticModel model, ISet<IMethodSymbol> candidates, CancellationToken cancellationToken);

        private partial class Editor
        {
            private INamedTypeSymbol GenerateNamedType()
            {
                return CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                    DetermineAttributes(),
                    DetermineAccessibility(),
                    DetermineModifiers(),
                    DetermineTypeKind(),
                    DetermineName(),
                    DetermineTypeParameters(),
                    DetermineBaseType(),
                    DetermineInterfaces(),
                    members: DetermineMembers());
            }

            private INamedTypeSymbol GenerateNamedType(GenerateTypeOptionsResult options)
            {
                if (options.TypeKind == TypeKind.Delegate)
                {
                    return CodeGenerationSymbolFactory.CreateDelegateTypeSymbol(
                        DetermineAttributes(),
                        options.Accessibility,
                        DetermineModifiers(),
                        DetermineReturnType(options),
                        options.TypeName,
                        DetermineTypeParameters(options),
                        DetermineParameters(options));
                }

                return CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                    DetermineAttributes(),
                    options.Accessibility,
                    DetermineModifiers(),
                    options.TypeKind,
                    options.TypeName,
                    DetermineTypeParameters(),
                    DetermineBaseType(),
                    DetermineInterfaces(),
                    members: DetermineMembers(options));
            }

            private ITypeSymbol DetermineReturnType(GenerateTypeOptionsResult options)
            {
                if (state.DelegateMethodSymbol == null ||
                    state.DelegateMethodSymbol.ReturnType == null ||
                    state.DelegateMethodSymbol.ReturnType is IErrorTypeSymbol)
                {
                    // Since we cannot determine the return type, we are returning void
                    return state.Compilation.GetSpecialType(SpecialType.System_Void);
                }
                else
                {
                    return state.DelegateMethodSymbol.ReturnType;
                }
            }

            private IList<ITypeParameterSymbol> DetermineTypeParameters(GenerateTypeOptionsResult options)
            {
                if (state.DelegateMethodSymbol != null)
                {
                    return state.DelegateMethodSymbol.TypeParameters;
                }

                // If the delegate symbol cannot be determined then 
                return DetermineTypeParameters();
            }

            private IList<IParameterSymbol> DetermineParameters(GenerateTypeOptionsResult options)
            {
                if (state.DelegateMethodSymbol != null)
                {
                    return state.DelegateMethodSymbol.Parameters;
                }

                return null;
            }

            private IList<ISymbol> DetermineMembers(GenerateTypeOptionsResult options = null)
            {
                var members = new List<ISymbol>();
                AddMembers(members, options);

                if (state.IsException)
                {
                    AddExceptionConstructors(members);
                }

                return members;
            }

            private void AddMembers(IList<ISymbol> members, GenerateTypeOptionsResult options = null)
            {
                AddProperties(members);

                IList<TArgumentSyntax> argumentList;
                if (!service.TryGetArgumentList(state.ObjectCreationExpressionOpt, out argumentList))
                {
                    return;
                }
                
                var parameterTypes = GetArgumentTypes(argumentList);

                // Don't generate this constructor if it would conflict with a default exception
                // constructor.  Default exception constructors will be added automatically by our
                // caller.
                if (state.IsException &&
                    state.BaseTypeOrInterfaceOpt.InstanceConstructors.Any(
                        c => c.Parameters.Select(p => p.Type).SequenceEqual(parameterTypes)))
                {
                    return;
                }

                // If there's an accessible base constructor that would accept these types, then
                // just call into that instead of generating fields.
                if (state.BaseTypeOrInterfaceOpt != null)
                {
                    if (state.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface && argumentList.Count == 0)
                    {
                        // No need to add the default constructor if our base type is going to be
                        // 'object'.  We get that constructor for free.
                        return;
                    }

                    var accessibleInstanceConstructors = state.BaseTypeOrInterfaceOpt.InstanceConstructors.Where(
                        IsSymbolAccessible).ToSet();

                    if (accessibleInstanceConstructors.Any())
                    {
                        var delegatedConstructor = service.GetDelegatingConstructor(state.ObjectCreationExpressionOpt, state.BaseTypeOrInterfaceOpt, document.SemanticModel, accessibleInstanceConstructors, cancellationToken);
                        if (delegatedConstructor != null)
                        {
                            // There was a best match.  Call it directly.  
                            AddBaseDelegatingConstructor(delegatedConstructor, members);
                            return;
                        }
                    }
                }

                // Otherwise, just generate a normal constructor that assigns any provided
                // parameters into fields.
                AddFieldDelegatingConstructor(argumentList, members, options);
            }

            private void AddProperties(IList<ISymbol> members)
            {
                var typeInference = document.Project.LanguageServices.GetService<ITypeInferenceService>();
                foreach (var property in state.PropertiesToGenerate)
                {
                    IPropertySymbol generatedProperty;
                    if (service.TryGenerateProperty(property, document.SemanticModel, typeInference, cancellationToken, out generatedProperty))
                    {
                        members.Add(generatedProperty);
                    }
                }
            }

            private void AddBaseDelegatingConstructor(
                IMethodSymbol methodSymbol,
                IList<ISymbol> members)
            {
                // If we're generating a constructor to delegate into the no-param base constructor
                // then we can just elide the constructor entirely.
                if (methodSymbol.Parameters.Length == 0)
                {
                    return;
                }

                var factory = this.document.Project.LanguageServices.GetService<SyntaxGenerator>();
                members.Add(factory.CreateBaseDelegatingConstructor(
                    methodSymbol, DetermineName()));
            }

            private void AddFieldDelegatingConstructor(
                IList<TArgumentSyntax> argumentList, IList<ISymbol> members, GenerateTypeOptionsResult options = null)
            {
                var factory = document.Project.LanguageServices.GetService<SyntaxGenerator>();
                var syntaxFactsService = document.Project.LanguageServices.GetService<ISyntaxFactsService>();

                var availableTypeParameters = service.GetAvailableTypeParameters(state, document.SemanticModel, intoNamespace, cancellationToken);
                var parameterTypes = GetArgumentTypes(argumentList);
                var parameterNames = service.GenerateParameterNames(document.SemanticModel, argumentList);
                var parameters = new List<IParameterSymbol>();

                var parameterToExistingFieldMap = new Dictionary<string, ISymbol>();
                var parameterToNewFieldMap = new Dictionary<string, string>();

                var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                for (var i = 0; i < parameterNames.Count; i++)
                {
                    var refKind = syntaxFacts.GetRefKindOfArgument(argumentList[i]);

                    var parameterName = parameterNames[i];
                    var parameterType = (ITypeSymbol)parameterTypes[i];
                    parameterType = parameterType.RemoveUnavailableTypeParameters(
                        this.document.SemanticModel.Compilation, availableTypeParameters);

                    if (!TryFindMatchingField(parameterName, parameterType, parameterToExistingFieldMap, caseSensitive: true))
                    {
                        if (!TryFindMatchingField(parameterName, parameterType, parameterToExistingFieldMap, caseSensitive: false))
                        {
                            parameterToNewFieldMap[parameterName] = parameterName;
                        }
                    }

                    parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes: null,
                        refKind: refKind,
                        isParams: false,
                        type: parameterType,
                        name: parameterName));
                }

                // Empty Constructor for Struct is not allowed
                if (!(parameters.Count == 0 && options != null && (options.TypeKind == TypeKind.Struct || options.TypeKind == TypeKind.Structure)))
                {
                    members.AddRange(factory.CreateFieldDelegatingConstructor(
                        DetermineName(), null, parameters, parameterToExistingFieldMap, parameterToNewFieldMap, cancellationToken));
                }
            }

            private void AddExceptionConstructors(IList<ISymbol> members)
            {
                var factory = document.Project.LanguageServices.GetService<SyntaxGenerator>();
                var exceptionType = document.SemanticModel.Compilation.ExceptionType();
                var constructors =
                   exceptionType.InstanceConstructors
                       .Where(c => c.DeclaredAccessibility == Accessibility.Public || c.DeclaredAccessibility == Accessibility.Protected)
                       .Select(c => CodeGenerationSymbolFactory.CreateConstructorSymbol(
                           attributes: null,
                           accessibility: c.DeclaredAccessibility,
                           modifiers: default(DeclarationModifiers),
                           typeName: DetermineName(),
                           parameters: c.Parameters,
                           statements: null,
                           baseConstructorArguments: c.Parameters.Length == 0 ? null : factory.CreateArguments(c.Parameters)));
                members.AddRange(constructors);
            }

            private IList<AttributeData> DetermineAttributes()
            {
                if (state.IsException)
                {
                    var serializableType = document.SemanticModel.Compilation.SerializableAttributeType();
                    if (serializableType != null)
                    {
                        var attribute = CodeGenerationSymbolFactory.CreateAttributeData(serializableType);
                        return new[] { attribute };
                    }
                }

                return null;
            }

            private Accessibility DetermineAccessibility()
            {
                return service.GetAccessibility(state, document.SemanticModel, intoNamespace, cancellationToken);
            }

            private DeclarationModifiers DetermineModifiers()
            {
                return default(DeclarationModifiers);
            }

            private INamedTypeSymbol DetermineBaseType()
            {
                if (state.BaseTypeOrInterfaceOpt == null || state.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface)
                {
                    return null;
                }

                return RemoveUnavailableTypeParameters(state.BaseTypeOrInterfaceOpt);
            }

            private IList<INamedTypeSymbol> DetermineInterfaces()
            {
                if (state.BaseTypeOrInterfaceOpt != null && state.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface)
                {
                    var type = RemoveUnavailableTypeParameters(state.BaseTypeOrInterfaceOpt);
                    if (type != null)
                    {
                        return new[] { type };
                    }
                }

                return SpecializedCollections.EmptyList<INamedTypeSymbol>();
            }

            private INamedTypeSymbol RemoveUnavailableTypeParameters(INamedTypeSymbol type)
            {
                return type.RemoveUnavailableTypeParameters(
                    document.SemanticModel.Compilation, GetAvailableTypeParameters()) as INamedTypeSymbol;
            }

            private string DetermineName()
            {
                return GetTypeName(state);
            }

            private IList<ITypeParameterSymbol> DetermineTypeParameters()
            {
                return service.GetTypeParameters(state, document.SemanticModel, cancellationToken);
            }

            private TypeKind DetermineTypeKind()
            {
                return state.IsStruct
                    ? TypeKind.Struct
                    : state.IsInterface
                        ? TypeKind.Interface
                        : TypeKind.Class;
            }

            protected IList<ITypeParameterSymbol> GetAvailableTypeParameters()
            {
                var availableInnerTypeParameters = service.GetTypeParameters(state, document.SemanticModel, cancellationToken);
                var availableOuterTypeParameters = !intoNamespace && state.TypeToGenerateInOpt != null
                    ? state.TypeToGenerateInOpt.GetAllTypeParameters()
                    : SpecializedCollections.EmptyEnumerable<ITypeParameterSymbol>();

                return availableOuterTypeParameters.Concat(availableInnerTypeParameters).ToList();
            }
        }

        internal abstract bool TryGenerateProperty(TSimpleNameSyntax propertyName, SemanticModel semanticModel, ITypeInferenceService typeInference, CancellationToken cancellationToken, out IPropertySymbol property);
    }
}
