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
            public IMethodSymbol ConstructorToAddTo { get; private set; }
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
                // We are trying to add these parameters into an existing constructor's parameter list.
                // Comparing parameters based on names to make sure parameter list won't contains duplicate parameters after we
                // append the new parameters
                this.ConstructorToAddTo = service.GetDelegatedConstructorBasedOnParameterNames(this.ContainingType, parameters);

                if (this.ConstructorToAddTo == null)
                {
                    return false;
                }

                var zippedParametersAndSelectedMembers = parameters.Zip(selectedMembers, (parameter, selectedMember) => (parameter, selectedMember));
                var missingParametersBuilder = ArrayBuilder<IParameterSymbol>.GetInstance();
                var missingMembersBuilder = ArrayBuilder<ISymbol>.GetInstance();
                var constructorParamNames = this.ConstructorToAddTo.Parameters.SelectAsArray(p => p.Name);
                foreach ((var parameter, var selectedMember) in zippedParametersAndSelectedMembers)
                {
                    if (!constructorParamNames.Contains(parameter.Name))
                    {
                        missingParametersBuilder.Add(parameter);
                        missingMembersBuilder.Add(selectedMember);
                    }
                }

                this.MissingParameters = missingParametersBuilder.ToImmutableAndFree();
                this.MissingMembers = missingMembersBuilder.ToImmutableAndFree();
                return true;
            }
        }
    }
}
