// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Differencing;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TextDiffing;

internal static class TextDifferencingServiceExtensions
{
    public static IHierarchicalDifferenceCollection DiffSourceTexts(this ITextDifferencingService diffService, SourceText oldText, SourceText newText, StringDifferenceOptions options)
    {
        var oldTextSnapshot = oldText.FindCorrespondingEditorTextSnapshot();
        var newTextSnapshot = newText.FindCorrespondingEditorTextSnapshot();
        var useSnapshots = oldTextSnapshot != null && newTextSnapshot != null;

        var diffResult = useSnapshots
            ? diffService.DiffSnapshotSpans(oldTextSnapshot!.GetFullSpan(), newTextSnapshot!.GetFullSpan(), options)
            : diffService.DiffStrings(oldText.ToString(), newText.ToString(), options);

        return diffResult;
    }
}
