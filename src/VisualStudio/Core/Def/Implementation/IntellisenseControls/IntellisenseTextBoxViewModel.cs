// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls
{
    internal readonly struct IntellisenseTextBoxViewModel
    {
        public readonly IVsTextView VsTextView;
        public readonly IWpfTextView WpfTextView;
        public IntellisenseTextBoxViewModel(IVsTextView vsTextView, IWpfTextView wpfTextView)
        {
            VsTextView = vsTextView;
            WpfTextView = wpfTextView;
        }
    }
}
