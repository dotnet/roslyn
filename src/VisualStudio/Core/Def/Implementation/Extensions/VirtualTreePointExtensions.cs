// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
{
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
}
