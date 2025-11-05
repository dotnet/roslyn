// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers;

using static GenerateFromMembersHelpers;

internal sealed partial class AddConstructorParametersFromMembersCodeRefactoringProvider
{
    private sealed class State
    {
        public ImmutableArray<ConstructorCandidate> ConstructorCandidates { get; private set; }

        [NotNull]
        public INamedTypeSymbol? ContainingType { get; private set; }

        public static async Task<State?> GenerateAsync(
            ImmutableArray<ISymbol> selectedMembers,
            Document document,
            CancellationToken cancellationToken)
        {
            var state = new State();
            if (!await state.TryInitializeAsync(
                selectedMembers, document, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            return state;
        }

        private async Task<bool> TryInitializeAsync(
            ImmutableArray<ISymbol> selectedMembers,
            Document document,
            CancellationToken cancellationToken)
        {
            ContainingType = selectedMembers[0].ContainingType;

            var rules = await document.GetNamingRulesAsync(cancellationToken).ConfigureAwait(false);
            var parametersForSelectedMembers = DetermineParameters(selectedMembers, rules);

            if (!selectedMembers.All(IsWritableInstanceFieldOrProperty) ||
                ContainingType == null ||
                ContainingType.TypeKind == TypeKind.Interface ||
                parametersForSelectedMembers.IsEmpty)
            {
                return false;
            }

            ConstructorCandidates = await GetConstructorCandidatesInfoAsync(
                ContainingType, document, parametersForSelectedMembers, cancellationToken).ConfigureAwait(false);

            return !ConstructorCandidates.IsEmpty;
        }

        /// <summary>
        /// Try to find all constructors in <paramref name="containingType"/> whose parameters
        /// are a subset of the selected members by comparing name.
        /// These constructors will not be considered as potential candidates:
        ///  - if the constructor's parameter list contains 'ref' or 'params'
        ///  - any constructor that has a params[] parameter
        ///  - deserialization constructor
        ///  - implicit default constructor
        /// </summary>
        private static async Task<ImmutableArray<ConstructorCandidate>> GetConstructorCandidatesInfoAsync(
            INamedTypeSymbol containingType,
            Document document,
            ImmutableArray<(IParameterSymbol parameter, ISymbol fieldOrProperty)> parametersForSelectedMembers,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<ConstructorCandidate>.GetInstance(out var applicableConstructors);

            foreach (var constructor in containingType.InstanceConstructors)
            {
                if (await IsApplicableConstructorAsync(
                        constructor, document, parametersForSelectedMembers.SelectAsArray(p => p.parameter.Name), cancellationToken).ConfigureAwait(false))
                {
                    applicableConstructors.Add(new(
                        constructor,
                        parametersForSelectedMembers.WhereAsArray(t => !constructor.Parameters.Any(p => t.parameter.Name == p.Name))));
                }
            }

            return applicableConstructors.ToImmutableAndClear();
        }

        private static async Task<bool> IsApplicableConstructorAsync(IMethodSymbol constructor, Document document, ImmutableArray<string> parameterNamesForSelectedMembers, CancellationToken cancellationToken)
        {
            var constructorParams = constructor.Parameters;

            if (constructorParams.Length == 2)
            {
                var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var deserializationConstructorCheck = new DeserializationConstructorCheck(compilation);
                if (deserializationConstructorCheck.IsDeserializationConstructor(constructor))
                {
                    return false;
                }
            }

            return constructorParams.All(parameter => parameter.RefKind == RefKind.None) &&
                !constructor.IsImplicitlyDeclared &&
                !constructorParams.Any(static p => p.IsParams) &&
                !SelectedMembersAlreadyExistAsParameters(parameterNamesForSelectedMembers, constructorParams);
        }

        private static bool SelectedMembersAlreadyExistAsParameters(ImmutableArray<string> parameterNamesForSelectedMembers, ImmutableArray<IParameterSymbol> constructorParams)
            => constructorParams.Length != 0 &&
            !parameterNamesForSelectedMembers.Except(constructorParams.Select(p => p.Name)).Any();
    }
}
