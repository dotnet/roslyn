// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Text.Adornments
{
    public sealed class ClassifiedTextRun
    {
        public string ClassificationTypeName { get; }

        public string Text { get; }

        public string? MarkerTagType { get; }

        public ClassifiedTextRunStyle Style { get; }

        public string? Tooltip { get; }

        public Action? NavigationAction { get; }

        public ClassifiedTextRun(string classificationTypeName, string text)
            : this(classificationTypeName, text, ClassifiedTextRunStyle.Plain)
        {
        }

        public ClassifiedTextRun(string classificationTypeName, string text, ClassifiedTextRunStyle style)
            : this(classificationTypeName, text, style, markerTagType: null)
        {
        }

        public ClassifiedTextRun(string classificationTypeName, string text, ClassifiedTextRunStyle style, string? markerTagType)
            : this(classificationTypeName, text, style, markerTagType, navigationAction: null, tooltip: null)
        {
        }

        public ClassifiedTextRun(string classificationTypeName, string text, ClassifiedTextRunStyle style, string? markerTagType, Action? navigationAction, string? tooltip = null)
        {
            ClassificationTypeName = classificationTypeName ?? throw new ArgumentNullException(nameof(classificationTypeName));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Style = style;
            MarkerTagType = markerTagType;
            NavigationAction = navigationAction;
            Tooltip = tooltip;
        }
    }
}
