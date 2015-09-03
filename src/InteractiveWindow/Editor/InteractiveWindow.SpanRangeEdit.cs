// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal partial class InteractiveWindow
    {
        private struct SpanRangeEdit
        {
            public readonly int Start;
            public readonly int End;
            public readonly object[] Replacement;

            public SpanRangeEdit(int start, int count, object[] replacement)
            {
                Start = start;
                End = start + count;
                Replacement = replacement;
            }
        }
    }
}