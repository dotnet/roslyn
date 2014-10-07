// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public partial class Solution
    {
        private sealed class LinkedFileMergeResult
        {
            public SourceText MergedSourceText { get; internal set; }
            public bool HasMergeConflicts { get; private set; }

            public LinkedFileMergeResult(SourceText mergedSourceText, bool hasMergeConflicts)
            {
                MergedSourceText = mergedSourceText;
                HasMergeConflicts = hasMergeConflicts;
            }
        }
    }
}