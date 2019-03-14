// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
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
                Document document)
            {
                var state = new State();
                if (!await state.TryInitializeAsync(service, selectedMembers, document).ConfigureAwait(true))
                {
                    return null;
                }

                return state;
            }

            private async Task<bool> TryInitializeAsync(
                AddConstructorParametersFromMembersCodeRefactoringProvider service,
                ImmutableArray<ISymbol> selectedMembers,
                Document document)
            {
                if (!selectedMembers.All(IsWritableInstanceFieldOrProperty))
                {
                    return false;
                }

                ContainingType = selectedMembers[0].ContainingType;
                if (ContainingType == null || ContainingType.TypeKind == TypeKind.Interface)
                {
                    return false;
                }

                var parametersForSelectedMembers = service.DetermineParameters(selectedMembers);
                // We are trying to add these parameters into an existing constructor's parameter list.
                // Comparing parameters based on names to make sure parameter list won't contains duplicate parameters after we
                // append the new parameters
                ConstructorCandidates = await GetConstructorCandidatesInfo(ContainingType, parametersForSelectedMembers, selectedMembers, document).ConfigureAwait(false);

                if (ConstructorCandidates.Count<ConstructorCandidate>() == 0)
                {
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Try to find all constructors in <paramref name="containingType"/> whose parameters is the subset of <paramref name="parametersForSelectedMembers"/> by comparing name.
            /// These constructors will not be considered as potential candidates 
            ///  - if the constructor's parameter list contains 'ref' or 'params'
            ///  - any constructor that has a params[] parameter
            ///  - deserialization constructor
            ///  - implicit default constructor
            /// </summary>
            private async Task<ImmutableArray<ConstructorCandidate>> GetConstructorCandidatesInfo(
                INamedTypeSymbol containingType,
                ImmutableArray<IParameterSymbol> parametersForSelectedMembers,
                ImmutableArray<ISymbol> selectedMembers,
                Document document)
            {
                var parameterNamesForSelectedMembers = parametersForSelectedMembers.SelectAsArray(p => p.Name);
                var applicableConstructors = ArrayBuilder<ConstructorCandidate>.GetInstance();
                var constructors = containingType.InstanceConstructors;
                foreach (var constructor in constructors)
                {
                    var constructorParams = constructor.Parameters;

                    if (constructorParams.Length == 2)
                    {
                        var compilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
                        var deserializationConstructorCheck = new DeserializationConstructorCheck(compilation);
                        if (deserializationConstructorCheck.IsDeserializationConstructor(constructor))
                        {
                            continue;
                        }
                    }

                    if (!constructorParams.All(parameter => parameter.RefKind == RefKind.None) ||
                        (constructorParams.Length == 0 && constructor.IsImplicitlyDeclared) ||
                        constructorParams.Any(p => p.IsParams) ||
                        SelectedMembersAlreadyExistAsParameters(parameterNamesForSelectedMembers, constructorParams))
                    {
                        continue;
                    }

                    var missingParametersBuilder = ArrayBuilder<IParameterSymbol>.GetInstance();
                    var missingMembersBuilder = ArrayBuilder<ISymbol>.GetInstance();
                    var constructorParamNames = constructor.Parameters.SelectAsArray(p => p.Name);
                    var zippedParametersAndSelectedMembers = parametersForSelectedMembers.Zip(selectedMembers, (parameter, selectedMember) => (parameter, selectedMember));
                    foreach ((var parameter, var selectedMember) in zippedParametersAndSelectedMembers)
                    {
                        if (!constructorParamNames.Contains(parameter.Name))
                        {
                            missingParametersBuilder.Add(parameter);
                            missingMembersBuilder.Add(selectedMember);
                        }
                    }

                    if (missingParametersBuilder != null)
                    {
                        applicableConstructors.Add(new ConstructorCandidate(constructor, missingMembersBuilder.ToImmutableAndFree(), missingParametersBuilder.ToImmutableAndFree()));
                    }
                }

                return applicableConstructors.ToImmutableAndFree();
            }

            private static bool SelectedMembersAlreadyExistAsParameters(ImmutableArray<string> parameterNamesForSelectedMembers, ImmutableArray<IParameterSymbol> constructorParams)
            {
                if (constructorParams.Length == 0)
                {
                    return false;
                }

                if (parameterNamesForSelectedMembers.Except(constructorParams.Select(p => p.Name)).Any())
                {
                    return false;
                }

                return true;
            }
        }
    }
}
