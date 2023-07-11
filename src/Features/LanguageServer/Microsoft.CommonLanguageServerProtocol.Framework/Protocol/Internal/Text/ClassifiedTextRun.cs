// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Roslyn.Text.Adornments
{
    public sealed class ClassifiedTextRun
    {
        public string ClassificationTypeName { get; }

        public string Text { get; }

        public string MarkerTagType { get; }

        public ClassifiedTextRunStyle Style { get; }

        public string Tooltip { get; }

        public Action NavigationAction { get; }

        public ClassifiedTextRun(string classificationTypeName, string text)
            : this(classificationTypeName, text, ClassifiedTextRunStyle.Plain)
        {
        }

        public ClassifiedTextRun(string classificationTypeName, string text, ClassifiedTextRunStyle style)
        {
            ClassificationTypeName = classificationTypeName ?? throw new ArgumentNullException("classificationTypeName");
            Text = text ?? throw new ArgumentNullException("text");
            Style = style;
        }

        public ClassifiedTextRun(string classificationTypeName, string text, ClassifiedTextRunStyle style, string markerTagType)
        {
            ClassificationTypeName = classificationTypeName ?? throw new ArgumentNullException("classificationTypeName");
            Text = text ?? throw new ArgumentNullException("text");
            MarkerTagType = markerTagType;
            Style = style;
        }

        public ClassifiedTextRun(string classificationTypeName, string text, Action navigationAction, string tooltip = null, ClassifiedTextRunStyle style = ClassifiedTextRunStyle.Plain)
        {
            ClassificationTypeName = classificationTypeName ?? throw new ArgumentNullException("classificationTypeName");
            Text = text ?? throw new ArgumentNullException("text");
            Style = style;
            NavigationAction = navigationAction ?? throw new ArgumentNullException("navigationAction");
            Tooltip = tooltip;
        }
    }
}