// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal class InterfacePullerWithQuickAction : AbstractMemberPullerWithQuickAction
    {
        protected override bool IsDeclarationAlreadyInTarget(
            INamedTypeSymbol targetSymbol,
            ISymbol userSelectedNodeSymbol)
        {
            var allMembers = targetSymbol.GetMembers();
            var comparer = SymbolEquivalenceComparer.Instance;
            foreach (var interfaceMember in allMembers)
            {
                var implementationOfMember = userSelectedNodeSymbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember);
                if (comparer.Equals(userSelectedNodeSymbol, implementationOfMember?.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
