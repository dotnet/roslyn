// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal class ClassPullerWithQuickAction : AbstractMemberPullerWithQuickAction
    {
        protected override bool IsDeclarationAlreadyInTarget(INamedTypeSymbol targetSymbol, ISymbol userSelectedNodeSymbol)
        {
            if (userSelectedNodeSymbol is IFieldSymbol fieldSymbol)
            {
                // If a member whose name is same as the selected one, then don't provide the refactoring option
                return targetSymbol.GetMembers(fieldSymbol.Name).Any(member => member.Kind == SymbolKind.Field);
            }
            else
            {
                var overrideMembersSet = new HashSet<ISymbol>();
                for (var symbol = userSelectedNodeSymbol; symbol != null; symbol = symbol.OverriddenMember())
                {
                    overrideMembersSet.Add(symbol);
                }
                
                var membersInTargetClass =
                    targetSymbol.GetMembers().Where(member =>
                    {
                        if (member is IMethodSymbol method)
                        {
                            return method.MethodKind == MethodKind.Ordinary;
                        }
                        else if (member.Kind == SymbolKind.Field)
                        {
                            return !member.IsImplicitlyDeclared;
                        }
                        else
                        {
                            return true;
                        }
                    });

                return overrideMembersSet.Intersect(membersInTargetClass, SymbolEquivalenceComparer.Instance).Any();
            }
        }
    }
}
