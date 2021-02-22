// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.Margin.InheritanceChainMargin
{
    public class InheritanceChainGlyphFactory : IGlyphFactory
    {
        public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        {
            return new InheritanceChainMargin();
        }
    }
}
