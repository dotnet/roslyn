// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;

internal abstract partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
{
    internal abstract class SignatureInfo(
        SemanticDocument document,
        State state)
    {
        protected readonly SemanticDocument Document = document;
        protected readonly State State = state;
        private ImmutableArray<ITypeParameterSymbol> _typeParameters;
        private IDictionary<ITypeSymbol, ITypeParameterSymbol> _typeArgumentToTypeParameterMap;

        public ImmutableArray<ITypeParameterSymbol> DetermineTypeParameters(CancellationToken cancellationToken)
        {
            return _typeParameters.IsDefault
                ? (_typeParameters = DetermineTypeParametersWorker(cancellationToken))
                : _typeParameters;
        }

        protected abstract ImmutableArray<ITypeParameterSymbol> DetermineTypeParametersWorker(CancellationToken cancellationToken);
        protected abstract RefKind DetermineRefKind(CancellationToken cancellationToken);

        public ValueTask<ITypeSymbol> DetermineReturnTypeAsync(CancellationToken cancellationToken)
        {
            var type = DetermineReturnTypeWorker(cancellationToken);
            if (State.IsInConditionalAccessExpression)
            {
                type = type.RemoveNullableIfPresent();
            }

            return FixTypeAsync(type, cancellationToken);
        }

        protected abstract ImmutableArray<ITypeSymbol> DetermineTypeArguments(CancellationToken cancellationToken);
        protected abstract ITypeSymbol DetermineReturnTypeWorker(CancellationToken cancellationToken);
        protected abstract ImmutableArray<RefKind> DetermineParameterModifiers(CancellationToken cancellationToken);
        protected abstract ImmutableArray<ITypeSymbol> DetermineParameterTypes(CancellationToken cancellationToken);
        protected abstract ImmutableArray<bool> DetermineParameterOptionality(CancellationToken cancellationToken);
        protected abstract ImmutableArray<ParameterName> DetermineParameterNames(CancellationToken cancellationToken);

        internal async ValueTask<IPropertySymbol> GeneratePropertyAsync(
            SyntaxGenerator factory,
            bool isAbstract, bool includeSetter,
            CancellationToken cancellationToken)
        {
            var accessibility = DetermineAccessibility(isAbstract);
            var getMethod = CodeGenerationSymbolFactory.CreateAccessorSymbol(
                attributes: default,
                accessibility: accessibility,
                statements: GenerateStatements(factory, isAbstract));

            var setMethod = includeSetter ? getMethod : null;

            return CodeGenerationSymbolFactory.CreatePropertySymbol(
                attributes: default,
                accessibility: accessibility,
                modifiers: new DeclarationModifiers(isStatic: State.IsStatic, isAbstract: isAbstract),
                type: await DetermineReturnTypeAsync(cancellationToken).ConfigureAwait(false),
                refKind: DetermineRefKind(cancellationToken),
                explicitInterfaceImplementations: default,
                name: State.IdentifierToken.ValueText,
                parameters: await DetermineParametersAsync(cancellationToken).ConfigureAwait(false),
                getMethod: getMethod,
                setMethod: setMethod);
        }

        public async ValueTask<IMethodSymbol> GenerateMethodAsync(
            SyntaxGenerator factory,
            bool isAbstract,
            CancellationToken cancellationToken)
        {
            var parameters = await DetermineParametersAsync(cancellationToken).ConfigureAwait(false);
            var returnType = await DetermineReturnTypeAsync(cancellationToken).ConfigureAwait(false);
            var isUnsafe = false;
            if (!State.IsContainedInUnsafeType)
            {
                isUnsafe = returnType.RequiresUnsafeModifier() || parameters.Any(static p => p.Type.RequiresUnsafeModifier());
            }

            var knownTypes = new KnownTaskTypes(Document.SemanticModel.Compilation);

            var method = CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: default,
                accessibility: DetermineAccessibility(isAbstract),
                modifiers: new DeclarationModifiers(
                    isStatic: State.IsStatic, isAbstract: isAbstract, isUnsafe: isUnsafe, isAsync: knownTypes.IsTaskLike(returnType)),
                returnType: returnType,
                refKind: DetermineRefKind(cancellationToken),
                explicitInterfaceImplementations: default,
                name: State.IdentifierToken.ValueText,
                typeParameters: DetermineTypeParameters(cancellationToken),
                parameters: parameters,
                statements: GenerateStatements(factory, isAbstract),
                handlesExpressions: default,
                returnTypeAttributes: default,
                methodKind: State.MethodKind);

            // Ensure no conflicts between type parameter names and parameter names.
            var languageServiceProvider = Document.Project.Solution.Services.GetLanguageServices(State.TypeToGenerateIn.Language);
            var syntaxFacts = languageServiceProvider.GetService<ISyntaxFactsService>();

            var equalityComparer = syntaxFacts.StringComparer;
            var reservedParameterNames = DetermineParameterNames(cancellationToken)
                .Select(p => p.BestNameForParameter)
                .ToSet(equalityComparer);

            var newTypeParameterNames = NameGenerator.EnsureUniqueness(
                method.TypeParameters.SelectAsArray(t => t.Name),
                n => !reservedParameterNames.Contains(n));

            return method.RenameTypeParameters(newTypeParameterNames);
        }

        private async ValueTask<ITypeSymbol> FixTypeAsync(
            ITypeSymbol typeSymbol,
            CancellationToken cancellationToken)
        {
            // A type can't refer to a type parameter that isn't available in the type we're
            // eventually generating into.
            var availableMethodTypeParameters = DetermineTypeParameters(cancellationToken);
            var availableTypeParameters = State.TypeToGenerateIn.GetAllTypeParameters();

            var compilation = Document.SemanticModel.Compilation;
            var allTypeParameters = availableMethodTypeParameters.Concat(availableTypeParameters);
            var availableTypeParameterNames = allTypeParameters.Select(t => t.Name).ToSet();

            var typeArgumentToTypeParameterMap = GetTypeArgumentToTypeParameterMap(cancellationToken);

            typeSymbol = typeSymbol.RemoveAnonymousTypes(compilation);
            typeSymbol = await ReplaceTypeParametersBasedOnTypeConstraintsAsync(
                Document.Project, typeSymbol, compilation, availableTypeParameterNames, cancellationToken).ConfigureAwait(false);
            return typeSymbol.RemoveUnavailableTypeParameters(compilation, allTypeParameters)
                             .RemoveUnnamedErrorTypes(compilation)
                             .SubstituteTypes(typeArgumentToTypeParameterMap, new TypeGenerator());
        }

        private IDictionary<ITypeSymbol, ITypeParameterSymbol> GetTypeArgumentToTypeParameterMap(
            CancellationToken cancellationToken)
        {
            return _typeArgumentToTypeParameterMap ??= CreateTypeArgumentToTypeParameterMap(cancellationToken);
        }

        private IDictionary<ITypeSymbol, ITypeParameterSymbol> CreateTypeArgumentToTypeParameterMap(
            CancellationToken cancellationToken)
        {
            var typeArguments = DetermineTypeArguments(cancellationToken);
            var typeParameters = DetermineTypeParameters(cancellationToken);

            // We use a nullability-ignoring comparer because top-level and nested nullability won't matter. If we are looking to replace
            // IEnumerable<string> with T, we want to replace IEnumerable<string?> whenever it appears in an argument or return type, partly because
            // there's no way to represent something like T-with-only-the-inner-thing-nullable. We could leave the entire argument as is, but we're suspecting
            // this is closer to the user's desire, even if it might require some tweaking after the fact.
            var result = new Dictionary<ITypeSymbol, ITypeParameterSymbol>(SymbolEqualityComparer.Default);

            for (var i = 0; i < typeArguments.Length; i++)
            {
                if (typeArguments[i] != null)
                {
                    result[typeArguments[i]] = typeParameters[i];
                }
            }

            return result;
        }

        private ImmutableArray<SyntaxNode> GenerateStatements(
            SyntaxGenerator factory,
            bool isAbstract)
        {
            var throwStatement = CodeGenerationHelpers.GenerateThrowStatement(factory, Document, "System.NotImplementedException");

            return isAbstract || State.TypeToGenerateIn.TypeKind == TypeKind.Interface || throwStatement == null
                ? default
                : ImmutableArray.Create(throwStatement);
        }

        private async ValueTask<ImmutableArray<IParameterSymbol>> DetermineParametersAsync(CancellationToken cancellationToken)
        {
            var modifiers = DetermineParameterModifiers(cancellationToken);
            var types = await SpecializedTasks.WhenAll(DetermineParameterTypes(cancellationToken).Select(t => FixTypeAsync(t, cancellationToken))).ConfigureAwait(false);
            var optionality = DetermineParameterOptionality(cancellationToken);
            var names = DetermineParameterNames(cancellationToken);

            using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(out var result);
            for (var i = 0; i < modifiers.Length; i++)
            {
                result.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default,
                    refKind: modifiers[i],
                    isParams: false,
                    isOptional: optionality[i],
                    type: types[i],
                    name: names[i].BestNameForParameter));
            }

            return result.ToImmutable();
        }

        private Accessibility DetermineAccessibility(bool isAbstract)
        {
            var containingType = State.ContainingType;

            // If we're generating into an interface, then we don't use any modifiers.
            if (State.TypeToGenerateIn.TypeKind != TypeKind.Interface)
            {
                // Otherwise, figure out what accessibility modifier to use and optionally
                // mark it as static.
                if (containingType.IsContainedWithin(State.TypeToGenerateIn))
                {
                    return isAbstract ? Accessibility.Protected : Accessibility.Private;
                }
                else if (DerivesFrom(containingType) && State.IsStatic)
                {
                    // NOTE(cyrusn): We only generate protected in the case of statics.  Consider
                    // the case where we're generating into one of our base types.  i.e.:
                    //
                    // class B : A { void Goo() { A a; a.Goo(); }
                    //
                    // In this case we can *not* mark the method as protected.  'B' can only
                    // access protected members of 'A' through an instance of 'B' (or a subclass
                    // of B).  It can not access protected members through an instance of the
                    // superclass.  In this case we need to make the method public or internal.
                    //
                    // However, this does not apply if the method will be static.  i.e.
                    // 
                    // class B : A { void Goo() { A.Goo(); }
                    //
                    // B can access the protected statics of A, and so we generate 'Goo' as
                    // protected.

                    // TODO: Code coverage
                    return Accessibility.Protected;
                }
                else if (containingType.ContainingAssembly.IsSameAssemblyOrHasFriendAccessTo(State.TypeToGenerateIn.ContainingAssembly))
                {
                    return Accessibility.Internal;
                }
                else
                {
                    // TODO: Code coverage
                    return Accessibility.Public;
                }
            }

            return Accessibility.NotApplicable;
        }

        private bool DerivesFrom(INamedTypeSymbol containingType)
        {
            return containingType.GetBaseTypes().Select(t => t.OriginalDefinition)
                                                .OfType<INamedTypeSymbol>()
                                                .Contains(State.TypeToGenerateIn);
        }
    }
}
