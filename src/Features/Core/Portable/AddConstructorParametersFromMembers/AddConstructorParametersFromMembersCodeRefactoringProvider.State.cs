// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
{
    internal partial class AddConstructorParametersFromMembersCodeRefactoringProvider
    {
        private class State
        {
            public ImmutableArray<ConstructorCandidate> ConstructorCandidates { get; private set; }
            public INamedTypeSymbol ContainingType { get; private set; }

            public static async Task<State> GenerateAsync(
                AddConstructorParametersFromMembersCodeRefactoringProvider service,
                ImmutableArray<ISymbol> selectedMembers,
                Document document,
                CancellationToken cancellationToken)
            {
                var state = new State();
                if (!await state.TryInitializeAsync(
                    service, selectedMembers, document, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private async Task<bool> TryInitializeAsync(
                AddConstructorParametersFromMembersCodeRefactoringProvider service,
                ImmutableArray<ISymbol> selectedMembers,
                Document document,
                CancellationToken cancellationToken)
            {
                ContainingType = selectedMembers[0].ContainingType;

                var rules = await document.GetNamingRulesAsync(FallbackNamingRules.RefactoringMatchLookupRules, cancellationToken).ConfigureAwait(false);
                var parametersForSelectedMembers = service.DetermineParameters(selectedMembers, rules);

                if (!selectedMembers.All(IsWritableInstanceFieldOrProperty) ||
                    ContainingType == null ||
                    ContainingType.TypeKind == TypeKind.Interface ||
                    parametersForSelectedMembers.IsEmpty)
                {
                    return false;
                }

                ConstructorCandidates = await GetConstructorCandidatesInfoAsync(
                    ContainingType, service, selectedMembers, document, parametersForSelectedMembers, cancellationToken).ConfigureAwait(false);

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
            private async Task<ImmutableArray<ConstructorCandidate>> GetConstructorCandidatesInfoAsync(
                INamedTypeSymbol containingType,
                AddConstructorParametersFromMembersCodeRefactoringProvider service,
                ImmutableArray<ISymbol> selectedMembers,
                Document document,
                ImmutableArray<IParameterSymbol> parametersForSelectedMembers,
                CancellationToken cancellationToken)
            {
                var applicableConstructors = ArrayBuilder<ConstructorCandidate>.GetInstance();

                foreach (var constructor in containingType.InstanceConstructors)
                {
                    if (await IsApplicableConstructorAsync(
                        constructor, document, parametersForSelectedMembers.SelectAsArray(p => p.Name), cancellationToken).ConfigureAwait(false))
                    {
                        applicableConstructors.Add(CreateConstructorCandidate(parametersForSelectedMembers, selectedMembers, constructor));
                    }
                }

                return applicableConstructors.ToImmutableAndFree();
            }

            private static async Task<bool> IsApplicableConstructorAsync(IMethodSymbol constructor, Document document, ImmutableArray<string> parameterNamesForSelectedMembers, CancellationToken cancellationToken)
            {
                var constructorParams = constructor.Parameters;

                if (constructorParams.Length == 2)
                {
                    var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var deserializationConstructorCheck = new DeserializationConstructorCheck(compilation);
                    if (deserializationConstructorCheck.IsDeserializationConstructor(constructor))
                    {
                        return false;
                    }
                }

                return constructorParams.All(parameter => parameter.RefKind == RefKind.None) &&
                    !constructor.IsImplicitlyDeclared &&
                    !constructorParams.Any(p => p.IsParams) &&
                    !SelectedMembersAlreadyExistAsParameters(parameterNamesForSelectedMembers, constructorParams);
            }

            private static bool SelectedMembersAlreadyExistAsParameters(ImmutableArray<string> parameterNamesForSelectedMembers, ImmutableArray<IParameterSymbol> constructorParams)
                => constructorParams.Length != 0 &&
                !parameterNamesForSelectedMembers.Except(constructorParams.Select(p => p.Name)).Any();

            private static ConstructorCandidate CreateConstructorCandidate(ImmutableArray<IParameterSymbol> parametersForSelectedMembers, ImmutableArray<ISymbol> selectedMembers, IMethodSymbol constructor)
            {
                var missingParametersBuilder = ArrayBuilder<IParameterSymbol>.GetInstance();
                var missingMembersBuilder = ArrayBuilder<ISymbol>.GetInstance();
                var constructorParamNames = constructor.Parameters.SelectAsArray(p => p.Name);
                var zippedParametersAndSelectedMembers =
                    parametersForSelectedMembers.Zip(selectedMembers, (parameter, selectedMember) => (parameter, selectedMember));

                foreach (var (parameter, selectedMember) in zippedParametersAndSelectedMembers)
                {
                    if (!constructorParamNames.Contains(parameter.Name))
                    {
                        missingParametersBuilder.Add(parameter);
                        missingMembersBuilder.Add(selectedMember);
                    }
                }

                return new ConstructorCandidate(
                    constructor, missingMembersBuilder.ToImmutableAndFree(), missingParametersBuilder.ToImmutableAndFree());
            }
        }
    }
}
