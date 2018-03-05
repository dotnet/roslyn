// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
{
    internal partial class AddConstructorParametersFromMembersCodeRefactoringProvider
    {
        private class State
        {
            public TextSpan TextSpan { get; private set; }
            public IMethodSymbol MatchingConstructor { get; private set; }
            public IMethodSymbol DelegatedConstructor { get; private set; }
            public INamedTypeSymbol ContainingType { get; private set; }
            public ImmutableArray<ISymbol> SelectedMembers { get; private set; }
            public ImmutableArray<IParameterSymbol> Parameters { get; private set; }

            public static State Generate(
                AddConstructorParametersFromMembersCodeRefactoringProvider service,
                Document document,
                TextSpan textSpan,
                ImmutableArray<ISymbol> selectedMembers,
                CancellationToken cancellationToken)
            {
                var state = new State();
                if (!state.TryInitialize(service, document, textSpan, selectedMembers, cancellationToken))
                {
                    return null;
                }

                return state;
            }

            private bool TryInitialize(
                AddConstructorParametersFromMembersCodeRefactoringProvider service,
                Document document,
                TextSpan textSpan,
                ImmutableArray<ISymbol> selectedMembers,
                CancellationToken cancellationToken)
            {
                if (!selectedMembers.All(IsWritableInstanceFieldOrProperty))
                {
                    return false;
                }

                this.TextSpan = textSpan;
                this.SelectedMembers = selectedMembers;
                this.ContainingType = this.SelectedMembers[0].ContainingType;
                if (this.ContainingType == null || this.ContainingType.TypeKind == TypeKind.Interface)
                {
                    return false;
                }

                this.Parameters = service.DetermineParameters(selectedMembers);

                this.MatchingConstructor = service.GetMatchingConstructor(this.ContainingType, this.Parameters);
                this.DelegatedConstructor = service.GetDelegatedConstructor(this.ContainingType, this.Parameters);
                return this.DelegatedConstructor != null;
            }
        }
    }
}
