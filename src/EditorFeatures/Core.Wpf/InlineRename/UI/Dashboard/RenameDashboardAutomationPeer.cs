// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    /// <summary>
    /// Custom AutomationPeer to announce that an Inline Rename session has begun.
    /// </summary>
    internal class RenameDashboardAutomationPeer : UserControlAutomationPeer
    {
        private readonly string _identifier;

        public RenameDashboardAutomationPeer(UserControl owner, string identifier) : base(owner)
            => _identifier = identifier;

        protected override bool HasKeyboardFocusCore()
            => true;

        protected override bool IsKeyboardFocusableCore()
            => true;

        protected override string GetNameCore()
            => string.Format(EditorFeaturesResources.An_inline_rename_session_is_active_for_identifier_0, _identifier);

        protected override AutomationControlType GetAutomationControlTypeCore()
            => AutomationControlType.Custom;
    }
}
