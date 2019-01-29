// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers
{
    internal partial class GenerateConstructorFromMembersCodeRefactoringProvider
    {
        private class State
        {
            public TextSpan TextSpan { get; private set; }
            public IMethodSymbol MatchingConstructor { get; private set; }
            public IMethodSymbol DelegatedConstructor { get; private set; }
            public INamedTypeSymbol ContainingType { get; private set; }
            public ImmutableArray<ISymbol> SelectedMembers { get; private set; }
            public ImmutableArray<IParameterSymbol> Parameters { get; private set; }

            public static State TryGenerate(
                GenerateConstructorFromMembersCodeRefactoringProvider service,
                Document document,
                TextSpan textSpan,
                INamedTypeSymbol containingType,
                ImmutableArray<ISymbol> selectedMembers,
                CancellationToken cancellationToken)
            {
                var state = new State();
                if (!state.TryInitialize(service, document, textSpan, containingType, selectedMembers, cancellationToken))
                {
                    return null;
                }

                return state;
            }

            private bool TryInitialize(
                GenerateConstructorFromMembersCodeRefactoringProvider service,
                Document document,
                TextSpan textSpan,
                INamedTypeSymbol containingType,
                ImmutableArray<ISymbol> selectedMembers,
                CancellationToken cancellationToken)
            {
                if (!selectedMembers.All(IsWritableInstanceFieldOrProperty))
                {
                    return false;
                }

                this.SelectedMembers = selectedMembers;
                this.ContainingType = containingType;
                this.TextSpan = textSpan;
                if (this.ContainingType == null || this.ContainingType.TypeKind == TypeKind.Interface)
                {
                    return false;
                }

                this.Parameters = service.DetermineParameters(selectedMembers);
                this.MatchingConstructor = GetMatchingConstructorBasedOnParameterTypes(this.ContainingType, this.Parameters);
                // We are going to create a new contructor and pass part of the parameters into DelegatedConstructor,
                // so parameters should be compared based on types since we don't want get a type mismatch error after the new constructor is genreated.
                this.DelegatedConstructor = GetDelegatedConstructorBasedOnParameterTypes(this.ContainingType, this.Parameters);
                return true;
            }

            private IMethodSymbol GetDelegatedConstructorBasedOnParameterTypes(
                INamedTypeSymbol containingType,
                ImmutableArray<IParameterSymbol> parameters)
            {
                var q =
                    from c in containingType.InstanceConstructors
                    orderby c.Parameters.Length descending
                    where c.Parameters.Length > 0 && c.Parameters.Length < parameters.Length
                    where c.Parameters.All(p => p.RefKind == RefKind.None) && !c.Parameters.Any(p => p.IsParams)
                    let constructorTypes = c.Parameters.Select(p => p.Type)
                    let symbolTypes = parameters.Take(c.Parameters.Length).Select(p => p.Type)
                    where constructorTypes.SequenceEqual(symbolTypes)
                    select c;

                return q.FirstOrDefault();
            }

            private IMethodSymbol GetMatchingConstructorBasedOnParameterTypes(INamedTypeSymbol containingType, ImmutableArray<IParameterSymbol> parameters)
                => containingType.InstanceConstructors.FirstOrDefault(c => MatchesConstructorBasedOnParameterTypes(c, parameters));

            private bool MatchesConstructorBasedOnParameterTypes(IMethodSymbol constructor, ImmutableArray<IParameterSymbol> parameters)
                => parameters.Select(p => p.Type).SequenceEqual(constructor.Parameters.Select(p => p.Type));
        }
    }
}
