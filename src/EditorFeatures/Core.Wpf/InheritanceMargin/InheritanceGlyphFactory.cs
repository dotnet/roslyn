// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin
{
    internal sealed class InheritanceGlyphFactory : IGlyphFactory
    {
        public UIElement? GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        {
            if (tag is InheritanceMarginTag inheritanceMarginTag)
            {
                var membersOnLine = inheritanceMarginTag.MembersOnLine;
                Contract.ThrowIfTrue(membersOnLine.IsEmpty);

                if (membersOnLine.Length == 1)
                {
                    var viewModel = new SingleMemberMarginViewModel(inheritanceMarginTag);
                    return new MarginGlyph.InheritanceMargin(viewModel);
                }
                else
                {
                    var viewModel = new MultipleMembersMarginViewModel(inheritanceMarginTag);
                    return new MarginGlyph.InheritanceMargin(viewModel);
                }
            }

            return null;
        }
    }
}
