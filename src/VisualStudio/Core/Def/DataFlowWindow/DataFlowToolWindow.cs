// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.DataFlowWindow
{
    [Guid("60a19d42-2dd7-43f3-be90-c7a9cb7d28f4")]
    internal class DataFlowToolWindow : ToolWindowPane
    {
        public DataFlowToolWindow()
        {
            this.Caption = "Data Flow Tool Window";
            Content = new TextBlock()
            {
                Text = "Testing"
            };
        }
    }
}
