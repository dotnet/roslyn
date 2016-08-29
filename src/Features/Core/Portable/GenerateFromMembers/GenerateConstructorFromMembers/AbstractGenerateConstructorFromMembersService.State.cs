﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateFromMembers.GenerateConstructorFromMembers
{
    internal abstract partial class AbstractGenerateConstructorFromMembersService<TService, TMemberDeclarationSyntax>
    {
        private class State
        {
            public TextSpan TextSpan { get; private set; }
            public IMethodSymbol DelegatedConstructor { get; private set; }
            public INamedTypeSymbol ContainingType { get; private set; }
            public IList<ISymbol> SelectedMembers { get; private set; }
            public List<IParameterSymbol> Parameters { get; private set; }

            public static State Generate(
                TService service,
                Document document,
                TextSpan textSpan,
                INamedTypeSymbol containingType,
                IList<ISymbol> selectedMembers,
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
                TService service,
                Document document,
                TextSpan textSpan,
                INamedTypeSymbol containingType,
                IList<ISymbol> selectedMembers,
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

                if (service.HasMatchingConstructor(this.ContainingType, this.Parameters))
                {
                    return false;
                }

                this.DelegatedConstructor = service.GetDelegatedConstructor(this.ContainingType, this.Parameters);
                return true;
            }
        }
    }
}
