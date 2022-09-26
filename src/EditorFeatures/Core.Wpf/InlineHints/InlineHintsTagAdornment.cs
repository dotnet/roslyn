// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal class InlineHintsTagAdornment : StackPanel, IAccessibleAdornmentControl
    {
        private readonly string _automationText;

        public InlineHintsTagAdornment(string content)
        {
            _automationText = content;
        }

        public string GetAutomationText()
        {
            return _automationText;
        }
    }
}
