// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TagSpanIntervalTree<TTag>
    {
        private class IntervalIntrospector : IIntervalIntrospector<TagNode>
        {
            public readonly ITextSnapshot Snapshot;

            public IntervalIntrospector(ITextSnapshot snapshot)
            {
                this.Snapshot = snapshot;
            }

            public int GetStart(TagNode value)
            {
                return value.GetStart(this.Snapshot);
            }

            public int GetLength(TagNode value)
            {
                return value.GetLength(this.Snapshot);
            }
        }
    }
}
