// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Services.Editor.Shared.Utilities
{
    [ExcludeFromCodeCoverage]
    internal class VisibleSpansChangedEventArgs : EventArgs
    {
        public ITextBuffer TextBuffer { get; private set; }
        public NormalizedSnapshotSpanCollection VisibleSpans { get; private set; }

        [Obsolete("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.")]
        public VisibleSpansChangedEventArgs(ITextBuffer textBuffer, NormalizedSnapshotSpanCollection visibleSpans)
        {
            this.TextBuffer = textBuffer;
            this.VisibleSpans = visibleSpans;
        }
    }
}
