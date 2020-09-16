// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                        DetermineReturnType(),
                        RefKind.None,
                        name: options.TypeName,
                        typeParameters: DetermineTypeParametersWithDelegateChecks(),
                        parameters: DetermineParameters());
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

            private ITypeSymbol DetermineReturnType()
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

            private ImmutableArray<ITypeParameterSymbol> DetermineTypeParametersWithDelegateChecks()
            {
                if (_state.DelegateMethodSymbol != null)
                {
                    return _state.DelegateMethodSymbol.TypeParameters;
                }

                // If the delegate symbol cannot be determined then 
                return DetermineTypeParameters();
            }

            private ImmutableArray<IParameterSymbol> DetermineParameters()
            {
                if (_state.DelegateMethodSymbol != null)
                {
                    return _state.DelegateMethodSymbol.Parameters;
                }

                return default;
            }

            private ImmutableArray<ISymbol> DetermineMembers(GenerateTypeOptionsResult options = null)
            {
                using var _ = ArrayBuilder<ISymbol>.GetInstance(out var members);
                AddMembers(members, options);

                if (_state.IsException)
                    AddExceptionConstructors(members);

                return members.ToImmutable();
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
                        c => c.Parameters.Select(p => p.Type).SequenceEqual(parameterTypes, SymbolEqualityComparer.Default)))
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
                using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(out var parameters);

                var parameterToExistingFieldMap = ImmutableDictionary.CreateBuilder<string, ISymbol>();
                var parameterToNewFieldMap = ImmutableDictionary.CreateBuilder<string, string>();

                var syntaxFacts = _semanticDocument.Document.GetLanguageService<ISyntaxFactsService>();
                for (var i = 0; i < parameterNames.Count; i++)
                {
                    var refKind = syntaxFacts.GetRefKindOfArgument(argumentList[i]);

                    var parameterName = parameterNames[i];
                    var parameterType = parameterTypes[i];
                    parameterType = parameterType.RemoveUnavailableTypeParameters(
                        _semanticDocument.SemanticModel.Compilation, availableTypeParameters);

                    FindExistingOrCreateNewMember(parameterName, parameterType, parameterToExistingFieldMap, parameterToNewFieldMap);

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
                    members.AddRange(factory.CreateMemberDelegatingConstructor(
                        _semanticDocument.SemanticModel,
                        DetermineName(), null, parameters.ToImmutable(),
                        parameterToExistingFieldMap.ToImmutable(),
                        parameterToNewFieldMap.ToImmutable(),
                        addNullChecks: false,
                        preferThrowExpression: false,
                        generateProperties: false,
                        isContainedInUnsafeType: false)); // Since we generated the type, we know its not unsafe
                }
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
                => _service.GetAccessibility(_state, _semanticDocument.SemanticModel, _intoNamespace, _cancellationToken);

            private static DeclarationModifiers DetermineModifiers()
                => default;

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
                if (_state.BaseTypeOrInterfaceOpt != null && _state.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface)
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
                => GetTypeName(_state);

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
