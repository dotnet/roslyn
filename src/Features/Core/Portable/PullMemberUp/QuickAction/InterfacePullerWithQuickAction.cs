// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal class InterfacePullerWithQuickAction : AbstractMemberPullerWithQuickAction
    {
        internal readonly static InterfacePullerWithQuickAction Instance = new InterfacePullerWithQuickAction();

        private InterfacePullerWithQuickAction()
        {
        }

        protected override bool IsSelectedMemberDeclarationAlreadyInDestination(
            INamedTypeSymbol destination,
            ISymbol selectedNode)
        {
            foreach (var interfaceMember in destination.GetMembers())
            {
                var implementationOfMember = selectedNode.ContainingType.FindImplementationForInterfaceMember(interfaceMember);
                if (SymbolEquivalenceComparer.Instance.Equals(selectedNode, implementationOfMember?.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
