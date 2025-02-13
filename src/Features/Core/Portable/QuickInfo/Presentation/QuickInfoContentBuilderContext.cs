// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.QuickInfo.Presentation;

internal sealed class QuickInfoContentBuilderContext(
    Document document,
    ClassificationOptions classificationOptions,
    LineFormattingOptions lineFormattingOptions,
    INavigationActionFactory? navigationActionFactory)
{
    public Document Document { get; } = document;
    public ClassificationOptions ClassificationOptions { get; } = classificationOptions;
    public LineFormattingOptions LineFormattingOptions { get; } = lineFormattingOptions;
    public INavigationActionFactory? NavigationActionFactory { get; } = navigationActionFactory;
}
