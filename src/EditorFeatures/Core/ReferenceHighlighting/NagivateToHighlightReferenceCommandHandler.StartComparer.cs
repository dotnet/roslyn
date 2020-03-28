﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
