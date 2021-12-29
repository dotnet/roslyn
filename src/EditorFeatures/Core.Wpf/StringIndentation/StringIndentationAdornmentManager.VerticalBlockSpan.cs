// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor.StringIndentation
{
    internal partial class StringIndentationAdornmentManager
    {
        private readonly struct VerticalBlockSpan
        {
            public readonly double Start;
            public readonly double End;

            public VerticalBlockSpan(double start, double end)
            {
                this.Start = start;
                this.End = end;
            }
        }
    }
}
