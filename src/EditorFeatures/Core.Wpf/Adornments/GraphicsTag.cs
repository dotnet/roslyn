// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Adornments
{
    /// <summary>
    /// This needs to be public for testing the AdornmentManager
    /// </summary>
    internal abstract class GraphicsTag : ITag
    {
        private readonly IEditorFormatMap _editorFormatMap;
        public Brush GraphicsTagBrush { get; private set; }
        private Color _graphicsTagColor;

        protected GraphicsTag(IEditorFormatMap editorFormatMap)
            => _editorFormatMap = editorFormatMap;

        public void Initialize(IWpfTextView view)
        {
            if (GraphicsTagBrush != null)
            {
                return;
            }

            // If we can't get the color for some reason, fall back to a hardcoded value
            // the editor has for outlining.
            var lightGray = Color.FromRgb(0xA5, 0xA5, 0xA5);

            var color = this.GetColor(view, _editorFormatMap) ?? lightGray;

            _graphicsTagColor = color;
            GraphicsTagBrush = new SolidColorBrush(_graphicsTagColor);
        }

        protected abstract Color? GetColor(IWpfTextView view, IEditorFormatMap editorFormatMap);

        /// <summary>
        /// This method allows corresponding adornment manager to ask for a graphical glyph.
        /// </summary>
        public abstract GraphicsResult GetGraphics(IWpfTextView view, Geometry bounds, TextFormattingRunProperties format);
    }
}
