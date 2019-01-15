// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
{
    internal partial class AddConstructorParametersFromMembersCodeRefactoringProvider
    {
        private class State
        {
            public IMethodSymbol DelegatedConstructor { get; private set; }
            public INamedTypeSymbol ContainingType { get; private set; }
            public ImmutableArray<ISymbol> MissingMembers { get; private set; }
            public ImmutableArray<IParameterSymbol> MissingParameters { get; private set; }

            public static State Generate(
                AddConstructorParametersFromMembersCodeRefactoringProvider service,
                ImmutableArray<ISymbol> selectedMembers)
            {
                var state = new State();
                if (!state.TryInitialize(service, selectedMembers))
                {
                    return null;
                }

                return state;
            }

            private bool TryInitialize(
                AddConstructorParametersFromMembersCodeRefactoringProvider service,
                ImmutableArray<ISymbol> selectedMembers)
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

                var parameters = service.DetermineParameters(selectedMembers);
                DelegatedConstructor = service.GetDelegatedConstructor(ContainingType, parameters);

                if (DelegatedConstructor != null)
                {
                    var zippedParametersAndSelectedMember = parameters.Zip(selectedMembers, (parameter, selectedMember) => (parameter, selectedMember));
                    var missingParamtersBuilder = ImmutableArray.CreateBuilder<IParameterSymbol>();
                    var missingMembersBuilder = ImmutableArray.CreateBuilder<ISymbol>();
                    foreach ((var parameter, var selectedMember) in zippedParametersAndSelectedMember)
                    {
                        if (IsParameterMissingFromConstructor(DelegatedConstructor, parameter))
                        {
                            missingParamtersBuilder.Add(parameter);
                            missingMembersBuilder.Add(selectedMember);
                        }
                    }

                    MissingParameters = missingParamtersBuilder.ToImmutableArray();
                    MissingMembers = missingMembersBuilder.ToImmutableArray();
                }

                return DelegatedConstructor != null;
            }

            /// <summary>
            /// Find whether <paramref name="parameter"/> is contained in <paramref name="constructor"/>'s parameter list by comparing name.
            /// </summary>
            private bool IsParameterMissingFromConstructor(IMethodSymbol constructor, IParameterSymbol parameter)
            {
                return !constructor.Parameters.Select(p => p.Name).Contains(parameter.Name);
            }
        }
    }
}
