// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
{
    internal static class ITextSnapshotExtensions
    {
        public static SnapshotSpan GetSpan(this ITextSnapshot snapshot, VsTextSpan textSpan)
        {
            return TryGetSpan(snapshot, textSpan).Value;
        }

        public static SnapshotSpan? TryGetSpan(this ITextSnapshot snapshot, VsTextSpan textSpan)
        {
            return snapshot.TryGetSpan(textSpan.iStartLine, textSpan.iStartIndex, textSpan.iEndLine, textSpan.iEndIndex);
        }
    }
}
