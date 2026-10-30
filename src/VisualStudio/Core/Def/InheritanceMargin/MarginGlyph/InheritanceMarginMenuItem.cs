// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;
using ShellControls = Microsoft.VisualStudio.Shell.Controls;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;

internal sealed class InheritanceMarginMenuItem : ShellControls.MenuItem
{
    protected override DependencyObject GetContainerForItemOverride()
        => new InheritanceMarginMenuItem();

    protected override AutomationPeer OnCreateAutomationPeer()
        => DataContext is HeaderMenuItemViewModel
            ? new HeaderAutomationPeer(this)
            : base.OnCreateAutomationPeer();

    // Section labels are informational text, not unavailable commands.
    private sealed class HeaderAutomationPeer(InheritanceMarginMenuItem owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()
            => nameof(InheritanceMarginMenuItem);

        protected override AutomationControlType GetAutomationControlTypeCore()
            => AutomationControlType.Text;

        protected override List<AutomationPeer>? GetChildrenCore()
            => null;
    }
}
