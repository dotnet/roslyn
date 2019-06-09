// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    /// <summary>
    /// Custom AutomationPeer to announce that an Inline Rename session has begun.
    /// </summary>
    internal class DashboardAutomationPeer : UserControlAutomationPeer
    {
        public DashboardAutomationPeer(UserControl owner) : base(owner)
        {
        }

        protected override bool HasKeyboardFocusCore()
        {
            return true;
        }

        protected override bool IsKeyboardFocusableCore()
        {
            return true;
        }

        protected override string GetNameCore()
        {
            return EditorFeaturesResources.An_inline_rename_session_is_active;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Pane;
        }
    }
}
