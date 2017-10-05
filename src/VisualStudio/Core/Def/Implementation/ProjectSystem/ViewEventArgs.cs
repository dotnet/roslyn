// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal class ViewEventArgs : EventArgs
    {
        private readonly IVsTextView _textView;

        public ViewEventArgs(IVsTextView textView)
        {
            _textView = textView;
        }

        public IVsTextView TextView { get { return _textView; } }
    }
}
