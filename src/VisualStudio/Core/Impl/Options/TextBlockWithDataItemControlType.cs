// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class TextBlockWithDataItemControlType : TextBlock
    {
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextBlockWithDataItemControlTypeAutomationPeer(this);
        }

        private class TextBlockWithDataItemControlTypeAutomationPeer : TextBlockAutomationPeer
        {
            public TextBlockWithDataItemControlTypeAutomationPeer(TextBlock owner) : base(owner)
            {
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.DataItem;
            }
        }
    }
}
