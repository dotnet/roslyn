// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
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
                        RefKind.None,
                        name: options.TypeName,
                        typeParameters: DetermineTypeParameters(options),
                        parameters: DetermineParameters(options));
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

            private ImmutableArray<ITypeParameterSymbol> DetermineTypeParameters(GenerateTypeOptionsResult options)
            {
                if (_state.DelegateMethodSymbol != null)
                {
                    return _state.DelegateMethodSymbol.TypeParameters;
                }

                // If the delegate symbol cannot be determined then 
                return DetermineTypeParameters();
            }

            private ImmutableArray<IParameterSymbol> DetermineParameters(GenerateTypeOptionsResult options)
            {
                if (_state.DelegateMethodSymbol != null)
                {
                    return _state.DelegateMethodSymbol.Parameters;
                }

                return default;
            }

            private ImmutableArray<ISymbol> DetermineMembers(GenerateTypeOptionsResult options = null)
            {
                var members = ArrayBuilder<ISymbol>.GetInstance();
                AddMembers(members, options);

                if (_state.IsException)
                {
                    AddExceptionConstructors(members);
                }

                return members.ToImmutableAndFree();
            }

            private void AddMembers(ArrayBuilder<ISymbol> members, GenerateTypeOptionsResult options = null)
            {
                AddProperties(members);
                if (!_service.TryGetArgumentList(_state.ObjectCreationExpressionOpt, out var argumentList))
                {
                    return;
                }

                var parameterTypes = GetArgumentTypes(argumentList);

                // Don't generate this constructor if it would conflict with a default exception
                // constructor.  Default exception constructors will be added automatically by our
                // caller.
                if (_state.IsException &&
                    _state.BaseTypeOrInterfaceOpt.InstanceConstructors.Any(
                        c => c.Parameters.Select(p => p.Type).SequenceEqual(parameterTypes, AllNullabilityIgnoringSymbolComparer.Instance)))
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
                            _semanticDocument,
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

            private void AddProperties(ArrayBuilder<ISymbol> members)
            {
                var typeInference = _semanticDocument.Document.GetLanguageService<ITypeInferenceService>();
                foreach (var property in _state.PropertiesToGenerate)
                {
                    if (_service.TryGenerateProperty(property, _semanticDocument.SemanticModel, typeInference, _cancellationToken, out var generatedProperty))
                    {
                        members.Add(generatedProperty);
                    }
                }
            }

            private void AddBaseDelegatingConstructor(
                IMethodSymbol methodSymbol,
                ArrayBuilder<ISymbol> members)
            {
                // If we're generating a constructor to delegate into the no-param base constructor
                // then we can just elide the constructor entirely.
                if (methodSymbol.Parameters.Length == 0)
                {
                    return;
                }

                var factory = _semanticDocument.Document.GetLanguageService<SyntaxGenerator>();
                members.Add(factory.CreateBaseDelegatingConstructor(
                    methodSymbol, DetermineName()));
            }

            private void AddFieldDelegatingConstructor(
                IList<TArgumentSyntax> argumentList, ArrayBuilder<ISymbol> members, GenerateTypeOptionsResult options = null)
            {
                var factory = _semanticDocument.Document.GetLanguageService<SyntaxGenerator>();

                var availableTypeParameters = _service.GetAvailableTypeParameters(_state, _semanticDocument.SemanticModel, _intoNamespace, _cancellationToken);
                var parameterTypes = GetArgumentTypes(argumentList);
                var parameterNames = _service.GenerateParameterNames(_semanticDocument.SemanticModel, argumentList, _cancellationToken);
                var parameters = ArrayBuilder<IParameterSymbol>.GetInstance();

                var parameterToExistingFieldMap = new Dictionary<string, ISymbol>();
                var parameterToNewFieldMap = new Dictionary<string, string>();

                var syntaxFacts = _semanticDocument.Document.GetLanguageService<ISyntaxFactsService>();
                for (var i = 0; i < parameterNames.Count; i++)
                {
                    var refKind = syntaxFacts.GetRefKindOfArgument(argumentList[i]);

                    var parameterName = parameterNames[i];
                    var parameterType = parameterTypes[i];
                    parameterType = parameterType.RemoveUnavailableTypeParameters(
                        _semanticDocument.SemanticModel.Compilation, availableTypeParameters);

                    if (!TryFindMatchingField(parameterName, parameterType, parameterToExistingFieldMap, caseSensitive: true))
                    {
                        if (!TryFindMatchingField(parameterName, parameterType, parameterToExistingFieldMap, caseSensitive: false))
                        {
                            parameterToNewFieldMap[parameterName.BestNameForParameter] = parameterName.NameBasedOnArgument;
                        }
                    }

                    parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes: default,
                        refKind: refKind,
                        isParams: false,
                        type: parameterType,
                        name: parameterName.BestNameForParameter));
                }

                // Empty Constructor for Struct is not allowed
                if (!(parameters.Count == 0 && options != null && (options.TypeKind == TypeKind.Struct || options.TypeKind == TypeKind.Structure)))
                {
                    var (fields, constructor) = factory.CreateFieldDelegatingConstructor(
                        _semanticDocument.SemanticModel,
                        DetermineName(), null, parameters.ToImmutable(),
                        parameterToExistingFieldMap, parameterToNewFieldMap,
                        addNullChecks: false, preferThrowExpression: false);
                    members.AddRange(fields);
                    members.Add(constructor);
                }

                parameters.Free();
            }

            private void AddExceptionConstructors(ArrayBuilder<ISymbol> members)
            {
                var factory = _semanticDocument.Document.GetLanguageService<SyntaxGenerator>();
                var exceptionType = _semanticDocument.SemanticModel.Compilation.ExceptionType();
                var constructors =
                   exceptionType.InstanceConstructors
                       .Where(c => c.DeclaredAccessibility == Accessibility.Public || c.DeclaredAccessibility == Accessibility.Protected)
                       .Select(c => CodeGenerationSymbolFactory.CreateConstructorSymbol(
                           attributes: default,
                           accessibility: c.DeclaredAccessibility,
                           modifiers: default,
                           typeName: DetermineName(),
                           parameters: c.Parameters,
                           statements: default,
                           baseConstructorArguments: c.Parameters.Length == 0
                                ? default
                                : factory.CreateArguments(c.Parameters)));
                members.AddRange(constructors);
            }

            private ImmutableArray<AttributeData> DetermineAttributes()
            {
                if (_state.IsException)
                {
                    var serializableType = _semanticDocument.SemanticModel.Compilation.SerializableAttributeType();
                    if (serializableType != null)
                    {
                        var attribute = CodeGenerationSymbolFactory.CreateAttributeData(serializableType);
                        return ImmutableArray.Create(attribute);
                    }
                }

                return default;
            }

            private Accessibility DetermineAccessibility()
            {
                return _service.GetAccessibility(_state, _semanticDocument.SemanticModel, _intoNamespace, _cancellationToken);
            }

            private DeclarationModifiers DetermineModifiers()
            {
                return default;
            }

            private INamedTypeSymbol DetermineBaseType()
            {
                if (_state.BaseTypeOrInterfaceOpt == null || _state.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface)
                {
                    return null;
                }

                return RemoveUnavailableTypeParameters(_state.BaseTypeOrInterfaceOpt);
            }

            private ImmutableArray<INamedTypeSymbol> DetermineInterfaces()
            {
                if (_state is { BaseTypeOrInterfaceOpt: { TypeKind: TypeKind.Interface } })
                {
                    var type = RemoveUnavailableTypeParameters(_state.BaseTypeOrInterfaceOpt);
                    if (type != null)
                    {
                        return ImmutableArray.Create(type);
                    }
                }

                return ImmutableArray<INamedTypeSymbol>.Empty;
            }

            private INamedTypeSymbol RemoveUnavailableTypeParameters(INamedTypeSymbol type)
            {
                return type.RemoveUnavailableTypeParameters(
                    _semanticDocument.SemanticModel.Compilation, GetAvailableTypeParameters()) as INamedTypeSymbol;
            }

            private string DetermineName()
            {
                return GetTypeName(_state);
            }

            private ImmutableArray<ITypeParameterSymbol> DetermineTypeParameters()
                => _service.GetTypeParameters(_state, _semanticDocument.SemanticModel, _cancellationToken);

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
                var availableInnerTypeParameters = _service.GetTypeParameters(_state, _semanticDocument.SemanticModel, _cancellationToken);
                var availableOuterTypeParameters = !_intoNamespace && _state.TypeToGenerateInOpt != null
                    ? _state.TypeToGenerateInOpt.GetAllTypeParameters()
                    : SpecializedCollections.EmptyEnumerable<ITypeParameterSymbol>();

                return availableOuterTypeParameters.Concat(availableInnerTypeParameters).ToList();
            }
        }

        internal abstract bool TryGenerateProperty(TSimpleNameSyntax propertyName, SemanticModel semanticModel, ITypeInferenceService typeInference, CancellationToken cancellationToken, out IPropertySymbol property);
    }
}
