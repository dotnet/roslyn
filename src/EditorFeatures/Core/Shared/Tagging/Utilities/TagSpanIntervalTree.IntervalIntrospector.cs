﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TagSpanIntervalTree<TTag>
    {
        private readonly struct IntervalIntrospector : IIntervalIntrospector<TagNode>
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
