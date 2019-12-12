// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
