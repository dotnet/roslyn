// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Text.Adornments;

internal sealed class ClassifiedTextRun(
    string classificationTypeName,
    string text,
    ClassifiedTextRunStyle style = ClassifiedTextRunStyle.Plain,
    string? markerTagType = null,
    Action? navigationAction = null,
    string? tooltip = null)
{
    public string ClassificationTypeName { get; } = classificationTypeName ?? throw new ArgumentNullException(nameof(classificationTypeName));
    public string Text { get; } = text ?? throw new ArgumentNullException(nameof(text));
    public string? MarkerTagType { get; } = markerTagType;
    public ClassifiedTextRunStyle Style { get; } = style;
    public string? Tooltip { get; } = tooltip;
    public Action? NavigationAction { get; } = navigationAction;
}
