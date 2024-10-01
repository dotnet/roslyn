// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;
using Microsoft.VisualStudio.Text;
using CodeAnalysisQuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem;
using IntellisenseQuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;

internal static class IntellisenseQuickInfoBuilder
{
    internal static async Task<IntellisenseQuickInfoItem> BuildItemAsync(
        ITrackingSpan trackingSpan,
        CodeAnalysisQuickInfoItem quickInfoItem,
        Document document,
        ClassificationOptions classificationOptions,
        LineFormattingOptions lineFormattingOptions,
        INavigationActionFactory navigationActionFactory,
        CancellationToken cancellationToken)
    {
        var context = new QuickInfoContentBuilderContext(document, classificationOptions, lineFormattingOptions, navigationActionFactory);
        var content = await QuickInfoContentBuilder.BuildInteractiveContentAsync(quickInfoItem, context, cancellationToken).ConfigureAwait(false);

        return new IntellisenseQuickInfoItem(trackingSpan, content.ToVsElement());
    }
}
