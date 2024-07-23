// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers;

internal abstract partial class AbstractGenerateConstructorFromMembersCodeRefactoringProvider
{
    private class State
    {
        public TextSpan TextSpan { get; private set; }
        public IMethodSymbol? MatchingConstructor { get; private set; }
        public IMethodSymbol? DelegatedConstructor { get; private set; }
        [NotNull]
        public INamedTypeSymbol? ContainingType { get; private set; }
        public ImmutableArray<ISymbol> SelectedMembers { get; private set; }
        public ImmutableArray<IParameterSymbol> Parameters { get; private set; }
        public bool IsContainedInUnsafeType { get; private set; }

        public Accessibility Accessibility { get; private set; }

        public static async Task<State?> TryGenerateAsync(
            AbstractGenerateConstructorFromMembersCodeRefactoringProvider service,
            Document document,
            TextSpan textSpan,
            INamedTypeSymbol containingType,
            Accessibility? desiredAccessibility,
            ImmutableArray<ISymbol> selectedMembers,
            CancellationToken cancellationToken)
        {
            var state = new State();
            if (!await state.TryInitializeAsync(service, document, textSpan, containingType, desiredAccessibility, selectedMembers, cancellationToken).ConfigureAwait(false))
                return null;

            return state;
        }

        private async Task<bool> TryInitializeAsync(
            AbstractGenerateConstructorFromMembersCodeRefactoringProvider service,
            Document document,
            TextSpan textSpan,
            INamedTypeSymbol containingType,
            Accessibility? desiredAccessibility,
            ImmutableArray<ISymbol> selectedMembers,
            CancellationToken cancellationToken)
        {
            var mappedMembers = selectedMembers.Select(m => TryMapToWritableInstanceFieldOrProperty(service, m, cancellationToken)).Distinct().ToImmutableArray();
            if (mappedMembers.Any(m => m is null))
                return false;

            SelectedMembers = mappedMembers!;

            ContainingType = containingType;
            Accessibility = desiredAccessibility ?? (ContainingType.IsAbstractClass() ? Accessibility.Protected : Accessibility.Public);
            TextSpan = textSpan;
            if (ContainingType == null || ContainingType.TypeKind == TypeKind.Interface)
                return false;

            IsContainedInUnsafeType = service.ContainingTypesOrSelfHasUnsafeKeyword(containingType);

            var rules = await document.GetNamingRulesAsync(cancellationToken).ConfigureAwait(false);
            Parameters = DetermineParameters(SelectedMembers, rules);
            MatchingConstructor = GetMatchingConstructorBasedOnParameterTypes(ContainingType, Parameters);
            // We are going to create a new contructor and pass part of the parameters into DelegatedConstructor, so
            // parameters should be compared based on types since we don't want get a type mismatch error after the
            // new constructor is generated.
            DelegatedConstructor = GetDelegatedConstructorBasedOnParameterTypes(ContainingType, Parameters);
            return true;
        }

        private static ISymbol? TryMapToWritableInstanceFieldOrProperty(
            AbstractGenerateConstructorFromMembersCodeRefactoringProvider service,
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            if (IsWritableInstanceFieldOrProperty(symbol))
                return symbol;

            if (symbol is IPropertySymbol property)
                return service.TryMapToWritableInstanceField(property, cancellationToken);

            return null;
        }

        private static IMethodSymbol? GetDelegatedConstructorBasedOnParameterTypes(
            INamedTypeSymbol containingType,
            ImmutableArray<IParameterSymbol> parameters)
        {
            var q =
                from c in containingType.InstanceConstructors
                orderby c.Parameters.Length descending
                where c.Parameters.Length > 0 && c.Parameters.Length < parameters.Length
                where c.Parameters.All(p => p.RefKind == RefKind.None) && !c.Parameters.Any(static p => p.IsParams)
                let constructorTypes = c.Parameters.Select(p => p.Type)
                let symbolTypes = parameters.Take(c.Parameters.Length).Select(p => p.Type)
                where constructorTypes.SequenceEqual(symbolTypes, SymbolEqualityComparer.Default)
                select c;

            return q.FirstOrDefault();
        }

        private static IMethodSymbol? GetMatchingConstructorBasedOnParameterTypes(INamedTypeSymbol containingType, ImmutableArray<IParameterSymbol> parameters)
            => containingType.InstanceConstructors.FirstOrDefault(c => MatchesConstructorBasedOnParameterTypes(c, parameters));

        private static bool MatchesConstructorBasedOnParameterTypes(IMethodSymbol constructor, ImmutableArray<IParameterSymbol> parameters)
            => parameters.Select(p => p.Type).SequenceEqual(constructor.Parameters.Select(p => p.Type));
    }
}
