// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin
{
    internal sealed class InheritanceGlyphFactory : IGlyphFactory
    {
        public UIElement? GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        {
            if (tag is InheritanceMarginTag inheritanceMarginTag)
            {
                var members = inheritanceMarginTag.MembersOnLine;
                if (members.Length == 1)
                {
                    return new SingleMemberMargin(inheritanceMarginTag);
                }
            }

            return null;
        }
    }
}
