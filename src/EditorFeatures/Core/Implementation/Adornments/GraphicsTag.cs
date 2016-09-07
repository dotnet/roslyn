// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
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
        private IEditorFormatMapService _editorFormatMapService;
        private IEditorFormatMap _editorFormatMap;

        protected virtual void Initialize(IWpfTextView view, 
            ref Brush graphicsTagBrush,
            ref Color graphicsTagColor)
        {
            if (graphicsTagBrush != null)
            {
                return;
            }

            Debug.Assert(_editorFormatMap == null);
            _editorFormatMap = _editorFormatMapService.GetEditorFormatMap("text");
            _editorFormatMap.FormatMappingChanged += OnFormatMappingChanged;

            // TODO: Refresh this when the user changes fonts and colors

            // If we can't get the color for some reason, fall back to a hardcoded value
            // the editor has for outlining.
            var lightGray = Color.FromRgb(0xA5, 0xA5, 0xA5);

            var color = this.GetColor(view, _editorFormatMap) ?? lightGray;

            graphicsTagColor = color;
            graphicsTagBrush = new SolidColorBrush(graphicsTagColor);
        }

        protected abstract Color? GetColor(IWpfTextView view, IEditorFormatMap editorFormatMap);

        private void OnFormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            _editorFormatMap.FormatMappingChanged -= OnFormatMappingChanged;
            _editorFormatMap = null;
            this.ClearCachedFormatData();
        }

        protected abstract void ClearCachedFormatData();

        /// <summary>
        /// This method allows corresponding adornment manager to ask for a graphical glyph.
        /// </summary>
        public abstract GraphicsResult GetGraphics(IWpfTextView view, Geometry bounds);

        internal void RegisterService(IEditorFormatMapService editorFormatMapService)
        {
            _editorFormatMapService = editorFormatMapService;
        }
    }
}