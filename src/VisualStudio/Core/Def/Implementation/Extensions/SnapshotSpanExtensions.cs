// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
{
    internal static class SnapshotSpanExtensions
    {
        public static VsTextSpan ToVsTextSpan(this SnapshotSpan snapshotSpan)
        {
            snapshotSpan.GetLinesAndColumns(out var startLine, out var startColumnIndex, out var endLine, out var endColumnIndex);

            return new VsTextSpan()
            {
                iStartLine = startLine,
                iStartIndex = startColumnIndex,
                iEndLine = endLine,
                iEndIndex = endColumnIndex
            };
        }
    }
}
