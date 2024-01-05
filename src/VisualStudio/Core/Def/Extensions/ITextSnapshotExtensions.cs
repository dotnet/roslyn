// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
{
    internal static class ITextSnapshotExtensions
    {
        public static SnapshotSpan GetSpan(this ITextSnapshot snapshot, VsTextSpan textSpan)
            => TryGetSpan(snapshot, textSpan).Value;

        public static SnapshotSpan? TryGetSpan(this ITextSnapshot snapshot, VsTextSpan textSpan)
            => snapshot.TryGetSpan(textSpan.iStartLine, textSpan.iStartIndex, textSpan.iEndLine, textSpan.iEndIndex);
    }
}
