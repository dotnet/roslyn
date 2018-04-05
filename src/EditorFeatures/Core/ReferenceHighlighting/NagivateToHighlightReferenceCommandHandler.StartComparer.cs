// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.ReferenceHighlighting
{
    internal partial class NavigateToHighlightReferenceCommandHandler
    {
        private class StartComparer : IComparer<SnapshotSpan>
        {
            public int Compare(SnapshotSpan x, SnapshotSpan y)
            {
                return x.Start.CompareTo(y.Start);
            }
        }
    }
}
