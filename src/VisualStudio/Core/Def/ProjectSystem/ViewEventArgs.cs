// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal class ViewEventArgs : EventArgs
{
    private readonly IVsTextView _textView;

    public ViewEventArgs(IVsTextView textView)
        => _textView = textView;

    public IVsTextView TextView { get { return _textView; } }
}
