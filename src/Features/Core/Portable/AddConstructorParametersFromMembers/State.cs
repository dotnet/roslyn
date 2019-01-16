// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

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

                this.ContainingType = selectedMembers[0].ContainingType;
                if (this.ContainingType == null || this.ContainingType.TypeKind == TypeKind.Interface)
                {
                    return false;
                }

                var parameters = service.DetermineParameters(selectedMembers);
                this.DelegatedConstructor = service.GetDelegatedConstructorBasedOnParameterNames(this.ContainingType, parameters);

                if (this.DelegatedConstructor != null)
                {
                    var zippedParametersAndSelectedMembers = parameters.Zip(selectedMembers, (parameter, selectedMember) => (parameter, selectedMember));
                    var missingParamtersBuilder = new ArrayBuilder<IParameterSymbol>();
                    var missingMembersBuilder = new ArrayBuilder<ISymbol>();
                    foreach ((var parameter, var selectedMember) in zippedParametersAndSelectedMembers)
                    {
                        if (IsParameterMissingFromConstructor(this.DelegatedConstructor, parameter))
                        {
                            missingParamtersBuilder.Add(parameter);
                            missingMembersBuilder.Add(selectedMember);
                        }
                    }

                    this.MissingParameters = missingParamtersBuilder.ToImmutableAndFree();
                    this.MissingMembers = missingMembersBuilder.ToImmutableAndFree();
                }

                return this.DelegatedConstructor != null;
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
