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
        internal abstract IMethodSymbol GetDelegatingConstructor(
            SemanticDocument document,
            TObjectCreationExpressionSyntax objectCreation,
            INamedTypeSymbol namedType,
            ISet<IMethodSymbol> candidates, 
            CancellationToken cancellationToken);

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
                if (_state.DelegateMethodSymbol == null ||
                    _state.DelegateMethodSymbol.ReturnType == null ||
                    _state.DelegateMethodSymbol.ReturnType is IErrorTypeSymbol)
                {
                    // Since we cannot determine the return type, we are returning void
                    return _state.Compilation.GetSpecialType(SpecialType.System_Void);
                }
                else
                {
                    return _state.DelegateMethodSymbol.ReturnType;
                }
            }

            private IList<ITypeParameterSymbol> DetermineTypeParameters(GenerateTypeOptionsResult options)
            {
                if (_state.DelegateMethodSymbol != null)
                {
                    return _state.DelegateMethodSymbol.TypeParameters;
                }

                // If the delegate symbol cannot be determined then 
                return DetermineTypeParameters();
            }

            private IList<IParameterSymbol> DetermineParameters(GenerateTypeOptionsResult options)
            {
                if (_state.DelegateMethodSymbol != null)
                {
                    return _state.DelegateMethodSymbol.Parameters;
                }

                return null;
            }

            private IList<ISymbol> DetermineMembers(GenerateTypeOptionsResult options = null)
            {
                var members = new List<ISymbol>();
                AddMembers(members, options);

                if (_state.IsException)
                {
                    AddExceptionConstructors(members);
                }

                return members;
            }

            private void AddMembers(IList<ISymbol> members, GenerateTypeOptionsResult options = null)
            {
                AddProperties(members);

                IList<TArgumentSyntax> argumentList;
                if (!_service.TryGetArgumentList(_state.ObjectCreationExpressionOpt, out argumentList))
                {
                    return;
                }

                var parameterTypes = GetArgumentTypes(argumentList);

                // Don't generate this constructor if it would conflict with a default exception
                // constructor.  Default exception constructors will be added automatically by our
                // caller.
                if (_state.IsException &&
                    _state.BaseTypeOrInterfaceOpt.InstanceConstructors.Any(
                        c => c.Parameters.Select(p => p.Type).SequenceEqual(parameterTypes)))
                {
                    return;
                }

                // If there's an accessible base constructor that would accept these types, then
                // just call into that instead of generating fields.
                if (_state.BaseTypeOrInterfaceOpt != null)
                {
                    if (_state.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface && argumentList.Count == 0)
                    {
                        // No need to add the default constructor if our base type is going to be
                        // 'object'.  We get that constructor for free.
                        return;
                    }

                    var accessibleInstanceConstructors = _state.BaseTypeOrInterfaceOpt.InstanceConstructors.Where(
                        IsSymbolAccessible).ToSet();

                    if (accessibleInstanceConstructors.Any())
                    {
                        var delegatedConstructor = _service.GetDelegatingConstructor( 
                            _document,
                            _state.ObjectCreationExpressionOpt,
                            _state.BaseTypeOrInterfaceOpt,
                            accessibleInstanceConstructors,
                            _cancellationToken);
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
                var typeInference = _document.Project.LanguageServices.GetService<ITypeInferenceService>();
                foreach (var property in _state.PropertiesToGenerate)
                {
                    IPropertySymbol generatedProperty;
                    if (_service.TryGenerateProperty(property, _document.SemanticModel, typeInference, _cancellationToken, out generatedProperty))
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

                var factory = _document.Project.LanguageServices.GetService<SyntaxGenerator>();
                members.Add(factory.CreateBaseDelegatingConstructor(
                    methodSymbol, DetermineName()));
            }

            private void AddFieldDelegatingConstructor(
                IList<TArgumentSyntax> argumentList, IList<ISymbol> members, GenerateTypeOptionsResult options = null)
            {
                var factory = _document.Project.LanguageServices.GetService<SyntaxGenerator>();
                var syntaxFactsService = _document.Project.LanguageServices.GetService<ISyntaxFactsService>();

                var availableTypeParameters = _service.GetAvailableTypeParameters(_state, _document.SemanticModel, _intoNamespace, _cancellationToken);
                var parameterTypes = GetArgumentTypes(argumentList);
                var parameterNames = _service.GenerateParameterNames(_document.SemanticModel, argumentList);
                var parameters = new List<IParameterSymbol>();

                var parameterToExistingFieldMap = new Dictionary<string, ISymbol>();
                var parameterToNewFieldMap = new Dictionary<string, string>();

                var syntaxFacts = _document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                for (var i = 0; i < parameterNames.Count; i++)
                {
                    var refKind = syntaxFacts.GetRefKindOfArgument(argumentList[i]);

                    var parameterName = parameterNames[i];
                    var parameterType = parameterTypes[i];
                    parameterType = parameterType.RemoveUnavailableTypeParameters(
                        _document.SemanticModel.Compilation, availableTypeParameters);

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
                        DetermineName(), null, parameters, parameterToExistingFieldMap, parameterToNewFieldMap, _cancellationToken));
                }
            }

            private void AddExceptionConstructors(IList<ISymbol> members)
            {
                var factory = _document.Project.LanguageServices.GetService<SyntaxGenerator>();
                var exceptionType = _document.SemanticModel.Compilation.ExceptionType();
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
                if (_state.IsException)
                {
                    var serializableType = _document.SemanticModel.Compilation.SerializableAttributeType();
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
                return _service.GetAccessibility(_state, _document.SemanticModel, _intoNamespace, _cancellationToken);
            }

            private DeclarationModifiers DetermineModifiers()
            {
                return default(DeclarationModifiers);
            }

            private INamedTypeSymbol DetermineBaseType()
            {
                if (_state.BaseTypeOrInterfaceOpt == null || _state.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface)
                {
                    return null;
                }

                return RemoveUnavailableTypeParameters(_state.BaseTypeOrInterfaceOpt);
            }

            private IList<INamedTypeSymbol> DetermineInterfaces()
            {
                if (_state.BaseTypeOrInterfaceOpt != null && _state.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface)
                {
                    var type = RemoveUnavailableTypeParameters(_state.BaseTypeOrInterfaceOpt);
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
                    _document.SemanticModel.Compilation, GetAvailableTypeParameters()) as INamedTypeSymbol;
            }

            private string DetermineName()
            {
                return GetTypeName(_state);
            }

            private IList<ITypeParameterSymbol> DetermineTypeParameters()
            {
                return _service.GetTypeParameters(_state, _document.SemanticModel, _cancellationToken);
            }

            private TypeKind DetermineTypeKind()
            {
                return _state.IsStruct
                    ? TypeKind.Struct
                    : _state.IsInterface
                        ? TypeKind.Interface
                        : TypeKind.Class;
            }

            protected IList<ITypeParameterSymbol> GetAvailableTypeParameters()
            {
                var availableInnerTypeParameters = _service.GetTypeParameters(_state, _document.SemanticModel, _cancellationToken);
                var availableOuterTypeParameters = !_intoNamespace && _state.TypeToGenerateInOpt != null
                    ? _state.TypeToGenerateInOpt.GetAllTypeParameters()
                    : SpecializedCollections.EmptyEnumerable<ITypeParameterSymbol>();

                return availableOuterTypeParameters.Concat(availableInnerTypeParameters).ToList();
            }
        }

        internal abstract bool TryGenerateProperty(TSimpleNameSyntax propertyName, SemanticModel semanticModel, ITypeInferenceService typeInference, CancellationToken cancellationToken, out IPropertySymbol property);
    }
}
