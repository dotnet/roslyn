// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class TextBlockWithDataItemControlType : TextBlock
    {
        protected override AutomationPeer OnCreateAutomationPeer()
            => new TextBlockWithDataItemControlTypeAutomationPeer(this);

        private class TextBlockWithDataItemControlTypeAutomationPeer : TextBlockAutomationPeer
        {
            public TextBlockWithDataItemControlTypeAutomationPeer(TextBlock owner) : base(owner)
            {
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
                => AutomationControlType.DataItem;
        }
    }
}
