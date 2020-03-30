// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Adornments
{
    /// <summary>
    /// This needs to be public for testing the AdornmentManager
    /// </summary>
    internal abstract class GraphicsTag : ITag
    {
        private readonly IEditorFormatMap _editorFormatMap;
        protected Brush _graphicsTagBrush;
        protected Color _graphicsTagColor;

        protected GraphicsTag(IEditorFormatMap editorFormatMap)
            => _editorFormatMap = editorFormatMap;

        protected virtual void Initialize(IWpfTextView view)
        {
            if (_graphicsTagBrush != null)
            {
                return;
            }

            // If we can't get the color for some reason, fall back to a hardcoded value
            // the editor has for outlining.
            var lightGray = Color.FromRgb(0xA5, 0xA5, 0xA5);

            var color = this.GetColor(view, _editorFormatMap) ?? lightGray;

            _graphicsTagColor = color;
            _graphicsTagBrush = new SolidColorBrush(_graphicsTagColor);
        }

        protected abstract Color? GetColor(IWpfTextView view, IEditorFormatMap editorFormatMap);

        /// <summary>
        /// This method allows corresponding adornment manager to ask for a graphical glyph.
        /// </summary>
        public abstract GraphicsResult GetGraphics(IWpfTextView view, Geometry bounds);
    }
}
