// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal class TagsChangedForBufferEventArgs : EventArgs
    {
        public readonly ITextBuffer Buffer;
        public readonly NormalizedSnapshotSpanCollection Spans;

        public TagsChangedForBufferEventArgs(ITextBuffer buffer, NormalizedSnapshotSpanCollection spans)
        {
            this.Buffer = buffer;
            this.Spans = spans;
        }
    }
}
