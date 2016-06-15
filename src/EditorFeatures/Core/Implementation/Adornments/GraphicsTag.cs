// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Adornments
{
    /// <summary>
    /// This needs to be public for testing the AdornmentManager
    /// </summary>
    internal abstract class GraphicsTag : ITag
    {
        protected static SolidColorBrush VerticalRuleBrush;
        protected static Color VerticalRuleColor;

        protected virtual void Initialize(IWpfTextView view)
        {
            if (VerticalRuleBrush != null)
            {
                return;
            }

            // TODO: Refresh this when the user changes fonts and colors

            // TODO: Get from resources
            var lightGray = Color.FromRgb(0xE0, 0xE0, 0xE0);

            var outliningForegroundBrush = view.VisualElement.TryFindResource("outlining.verticalrule.foreground") as SolidColorBrush;
            var color = outliningForegroundBrush?.Color ?? lightGray;

            VerticalRuleColor = color;
            VerticalRuleBrush = new SolidColorBrush(VerticalRuleColor);
        }

        /// <summary>
        /// This method allows corresponding adornment manager to ask for a graphical glyph.
        /// </summary>
        public abstract GraphicsResult GetGraphics(IWpfTextView view, Geometry bounds);
    }
}