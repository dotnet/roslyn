// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Adornments
{
    internal abstract class BrushTag : ITag
    {
        private static readonly Color s_lightGray = Color.FromRgb(0xA5, 0xA5, 0xA5);
        private readonly IEditorFormatMap _editorFormatMap;

        private Brush? _brush;

        protected BrushTag(IEditorFormatMap editorFormatMap)
            => _editorFormatMap = editorFormatMap;

        public Brush GetBrush(IWpfTextView view)
            // If we can't get the color for some reason, fall back to a hard-coded value the editor has for outlining.
            => _brush ??= new SolidColorBrush(this.GetColor(view, _editorFormatMap) ?? s_lightGray);

        protected abstract Color? GetColor(IWpfTextView view, IEditorFormatMap editorFormatMap);
    }
}
