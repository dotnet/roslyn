// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal class ClassPullerWithQuickAction : AbstractMemberPullerWithQuickAction
    {
        internal readonly static ClassPullerWithQuickAction Instance = new ClassPullerWithQuickAction();

        private ClassPullerWithQuickAction()
        {
        }

        protected override bool IsSelectedMemberDeclarationAlreadyInDestination(INamedTypeSymbol destination, ISymbol selectedMember)
        {
            if (selectedMember is IFieldSymbol fieldSymbol)
            {
                // If there is a member with same name in destination, pull the selected field will cause error,
                // so don't provide refactoring under this scenario
                return destination.GetMembers(fieldSymbol.Name).Any();
            }
            else
            {
                var overrideMembersSet = new HashSet<ISymbol>();
                for (var symbol = selectedMember; symbol != null; symbol = symbol.OverriddenMember())
                {
                    overrideMembersSet.Add(symbol);
                }
                
                // Since the destination and selectedMember may belong different language, so use SymbolEquivalenceComparer as comparer
                return overrideMembersSet.Intersect(destination.GetMembers(), SymbolEquivalenceComparer.Instance).Any();
            }
        }
    }
}
