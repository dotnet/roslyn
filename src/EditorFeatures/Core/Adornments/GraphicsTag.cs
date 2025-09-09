// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Adornments;

internal abstract class GraphicsTag : BrushTag
{
    protected GraphicsTag(IEditorFormatMap editorFormatMap) : base(editorFormatMap)
    {
    }

    /// <summary>
    /// This method allows corresponding adornment manager to ask for a graphical glyph.
    /// </summary>
    public abstract GraphicsResult GetGraphics(IWpfTextView view, Geometry bounds, TextFormattingRunProperties format);
}
