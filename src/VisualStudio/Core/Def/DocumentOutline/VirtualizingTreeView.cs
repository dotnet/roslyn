// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline;

// Provide a workaround for https://github.com/dotnet/wpf/issues/122 when using virtualized TreeView.
internal sealed class VirtualizingTreeView : TreeView
{
    public VirtualizingTreeView()
    {
        // Two important properties are being set for TreeView:
        // We set IsVirtualizing to "true" so that WPF only generates internal data-structures elements that are visible.
        // Setting VirtualizationMode to "Recycling" ensures that WPF internal data is reused as items scroll in and out of view.
        VirtualizingPanel.SetIsVirtualizing(this, true);
        VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
    }

    protected override AutomationPeer OnCreateAutomationPeer()
        => new VirtualizingTreeViewAutomationPeer(this);

    public sealed class VirtualizingTreeViewAutomationPeer(TreeView owner)
        : TreeViewAutomationPeer(owner)
    {
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
            => new VirtualizingTreeViewDataItemAutomationPeer(item, this, null);
    }

    public sealed class VirtualizingTreeViewDataItemAutomationPeer(object item, ItemsControlAutomationPeer itemsControlAutomationPeer, TreeViewDataItemAutomationPeer? parentDataItemAutomationPeer)
        : TreeViewDataItemAutomationPeer(item, itemsControlAutomationPeer, parentDataItemAutomationPeer)
    {
        protected override string GetNameCore()
        {
            try
            {
                return base.GetNameCore();
            }
            catch (NullReferenceException)
            {
                // https://github.com/dotnet/wpf/issues/122
                return "";
            }
        }
    }
}
