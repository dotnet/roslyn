// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;

internal static class VirtualTreePointExtensions
{
    public static VsTextSpan ToVsTextSpan(this VirtualTreePoint virtualSnapshotPoint)
    {
        var line = virtualSnapshotPoint.GetContainingLine();
        var lineNumber = line.LineNumber;
        var columnIndex = virtualSnapshotPoint.Position - line.Start;
        columnIndex += virtualSnapshotPoint.VirtualSpaces;

        return new VsTextSpan()
        {
            iStartLine = lineNumber,
            iStartIndex = columnIndex,
            iEndLine = lineNumber,
            iEndIndex = columnIndex,
        };
    }
}
