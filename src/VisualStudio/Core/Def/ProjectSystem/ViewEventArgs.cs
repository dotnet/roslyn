// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal sealed class ViewEventArgs : EventArgs
{
    public ViewEventArgs(IVsTextView textView)
        => TextView = textView;

    public IVsTextView TextView { get; }
}
