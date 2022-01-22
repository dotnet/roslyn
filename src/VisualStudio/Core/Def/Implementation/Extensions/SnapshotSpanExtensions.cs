// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
