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
            snapshotSpan.GetLinesAndCharacters(out var startLine, out var startCharacterIndex, out var endLine, out var endCharacterIndex);

            return new VsTextSpan()
            {
                iStartLine = startLine,
                iStartIndex = startCharacterIndex,
                iEndLine = endLine,
                iEndIndex = endCharacterIndex
            };
        }
    }
}
