// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal class ClassPullerWithQuickAction : AbstractMemberPullerWithQuickAction
    {
        protected override bool IsDeclarationAlreadyInTarget(INamedTypeSymbol targetSymbol, ISymbol userSelectedNodeSymbol)
        {
            if (userSelectedNodeSymbol is IFieldSymbol fieldSymbol)
            {
                return targetSymbol.GetMembers().Any(member => member.Name == fieldSymbol.Name);
            }
            else
            {
                var overrideMethodSet = new HashSet<ISymbol>();
                if (userSelectedNodeSymbol is IMethodSymbol methodSymbol)
                {
                    for (var symbol = methodSymbol.OverriddenMethod; symbol != null; symbol = symbol.OverriddenMethod)
                    {
                        overrideMethodSet.Add(symbol);
                    }
                }
                else if (userSelectedNodeSymbol is IPropertySymbol propertySymbol)
                {
                    for (var symbol = propertySymbol.OverriddenProperty; symbol != null; symbol = symbol.OverriddenProperty)
                    {
                        overrideMethodSet.Add(symbol);
                    }
                }
                else if (userSelectedNodeSymbol is IEventSymbol eventSymbol)
                {
                    for (var symbol = eventSymbol.OverriddenEvent; symbol != null; symbol = symbol.OverriddenEvent)
                    {
                        overrideMethodSet.Add(symbol);
                    }
                }
                else
                {
                    throw new ArgumentException($"{userSelectedNodeSymbol} should be method, property or event");
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

                return overrideMethodSet.Intersect(membersInTargetClass).Any();
            }
        }
    }
}
