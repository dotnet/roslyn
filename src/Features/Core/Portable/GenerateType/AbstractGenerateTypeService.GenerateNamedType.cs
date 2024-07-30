// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateType;

internal abstract partial class AbstractGenerateTypeService<TService, TSimpleNameSyntax, TObjectCreationExpressionSyntax, TExpressionSyntax, TTypeDeclarationSyntax, TArgumentSyntax>
{
    private partial class Editor
    {
        private async Task<INamedTypeSymbol> GenerateNamedTypeAsync()
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
                members: await DetermineMembersAsync().ConfigureAwait(false));
        }

        private async Task<INamedTypeSymbol> GenerateNamedTypeAsync(GenerateTypeOptionsResult options)
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
                members: await DetermineMembersAsync(options).ConfigureAwait(false));
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

        private async Task<ImmutableArray<ISymbol>> DetermineMembersAsync(GenerateTypeOptionsResult options = null)
        {
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var members);
            await AddMembersAsync(members, options).ConfigureAwait(false);

            if (_state.IsException)
                AddExceptionConstructors(members);

            return members.ToImmutableAndClear();
        }

        private async Task AddMembersAsync(ArrayBuilder<ISymbol> members, GenerateTypeOptionsResult options = null)
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
                    static (c, parameterTypes) => c.Parameters.Select(p => p.Type).SequenceEqual(parameterTypes, SymbolEqualityComparer.Default), parameterTypes))
            {
                return;
            }

            // If there's an accessible base constructor that would accept these types, then
            // just call into that instead of generating fields.
            if (_state.BaseTypeOrInterfaceOpt != null)
            {
                if (_state.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface || argumentList.Count == 0)
                {
                    // No need to add the default constructor if our base type is going to be 'object' or if we
                    // would be calling the empty constructor.  We get that base constructor implicitly.
                    return;
                }

                // Synthesize some parameter symbols so we can see if these particular parameters could map to the
                // parameters of any of the constructors we have in our base class.  This will have the added
                // benefit of allowing us to infer better types for complex type-less expressions (like lambdas).
                var syntaxFacts = _semanticDocument.Document.GetLanguageService<ISyntaxFactsService>();
                var refKinds = argumentList.SelectAsArray(syntaxFacts.GetRefKindOfArgument);
                var parameters = parameterTypes.Zip(refKinds,
                    (t, r) => CodeGenerationSymbolFactory.CreateParameterSymbol(r, t, name: "")).ToImmutableArray();

                var expressions = GetArgumentExpressions(argumentList);
                var delegatedConstructor = _state.BaseTypeOrInterfaceOpt.InstanceConstructors.FirstOrDefault(
                    c => GenerateConstructorHelpers.CanDelegateTo(_semanticDocument, parameters, expressions, c));

                if (delegatedConstructor != null)
                {
                    // There was a constructor match in the base class.  Synthesize a constructor of our own with
                    // the same parameter types that calls into that.
                    var factory = _semanticDocument.Document.GetLanguageService<SyntaxGenerator>();
                    members.Add(factory.CreateBaseDelegatingConstructor(delegatedConstructor, DetermineName()));
                    return;
                }
            }

            // Otherwise, just generate a normal constructor that assigns any provided
            // parameters into fields.
            await AddFieldDelegatingConstructorAsync(argumentList, members, options).ConfigureAwait(false);
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

        private async Task AddFieldDelegatingConstructorAsync(
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

                await FindExistingOrCreateNewMemberAsync(parameterName, parameterType, parameterToExistingFieldMap, parameterToNewFieldMap).ConfigureAwait(false);

                parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default,
                    refKind: refKind,
                    isParams: false,
                    type: parameterType,
                    name: parameterName.BestNameForParameter));
            }

            // Empty Constructor for Struct is not allowed
            if (!(parameters.Count == 0 && options is { TypeKind: TypeKind.Struct }))
            {
                members.AddRange(factory.CreateMemberDelegatingConstructor(
                    factory.SyntaxGeneratorInternal,
                    _semanticDocument.SemanticModel,
                    DetermineName(), null, parameters.ToImmutable(), Accessibility.Public,
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
                   .Where(c => c.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected && !c.IsObsolete())
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
                    return [attribute];
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
                    return [type];
                }
            }

            return [];
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
                : [];

            return availableOuterTypeParameters.Concat(availableInnerTypeParameters).ToList();
        }
    }

    internal abstract bool TryGenerateProperty(TSimpleNameSyntax propertyName, SemanticModel semanticModel, ITypeInferenceService typeInference, CancellationToken cancellationToken, out IPropertySymbol property);
}
