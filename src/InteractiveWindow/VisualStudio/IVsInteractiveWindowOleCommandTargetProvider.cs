// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    /// <summary>
    /// The implementer is given a chance to attach a command filter that routes language services
    /// commands into the Interactive Window command filter chain.
    /// </summary>
    public interface IVsInteractiveWindowOleCommandTargetProvider
    {
        IOleCommandTarget GetCommandTarget(IWpfTextView textView, IOleCommandTarget nextTarget);
    }
}
