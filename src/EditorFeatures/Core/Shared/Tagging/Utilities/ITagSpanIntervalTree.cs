// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal interface ITagSpanIntervalTree<TTag> where TTag : ITag
    {
        IList<ITagSpan<TTag>> GetIntersectingSpans(SnapshotSpan snapshotSpan);
    }
}
