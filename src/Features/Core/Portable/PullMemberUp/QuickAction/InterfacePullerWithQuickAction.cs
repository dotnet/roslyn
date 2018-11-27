// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.PullMemberUp.QuickAction
{
    internal class InterfacePullerWithQuickAction : AbstractMemberPullerWithQuickAction
    {
        private static InterfacePullerWithQuickAction s_puller;

        internal static InterfacePullerWithQuickAction GetInstance
        {
            get
            {
                if (s_puller == null)
                {
                    s_puller = new InterfacePullerWithQuickAction();
                }
                return s_puller;
            }
        }

        private InterfacePullerWithQuickAction()
        {
        }

        protected override bool IsSelectedMemberDeclarationAlreadyInDestination(
            INamedTypeSymbol destination,
            ISymbol selectedNode)
        {
            var comparer = SymbolEquivalenceComparer.Instance;
            foreach (var interfaceMember in destination.GetMembers())
            {
                var implementationOfMember = selectedNode.ContainingType.FindImplementationForInterfaceMember(interfaceMember);
                if (comparer.Equals(selectedNode, implementationOfMember?.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
