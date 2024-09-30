// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.QuickInfo.Presentation;

internal readonly struct QuickInfoClassifiedTextRun(
    string classificationTypeName,
    string text,
    Action? navigationAction = null,
    string? tooltip = null,
    QuickInfoClassifiedTextStyle style = QuickInfoClassifiedTextStyle.Plain)
{
    // Note: MarkerTagType was not included from the VS ClassifiedTextRun
    // because Roslyn doesn't create ClassifiedTextRuns with that value.
    // If we eventually need that data, it should be added below.

    public string ClassificationTypeName { get; } = classificationTypeName;
    public string Text { get; } = text;
    public QuickInfoClassifiedTextStyle Style { get; } = style;
    public string? Tooltip { get; } = tooltip;
    public Action? NavigationAction { get; } = navigationAction;

    public QuickInfoClassifiedTextRun(
        string classificationTypeName,
        string text,
        QuickInfoClassifiedTextStyle style)
        : this(classificationTypeName, text, navigationAction: null, tooltip: null, style)
    {
    }
}
