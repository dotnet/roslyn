// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators
{
    /// <summary>
    /// This needs to be public for testing the AdornmentManager
    /// </summary>
    internal abstract class GraphicsTag : ITag
    {
        /// <summary>
        /// This method allows corresponding adornment manager to ask for a graphical glyph.
        /// </summary>
        public abstract GraphicsResult GetGraphics(IWpfTextView view, Geometry bounds);
    }
}
