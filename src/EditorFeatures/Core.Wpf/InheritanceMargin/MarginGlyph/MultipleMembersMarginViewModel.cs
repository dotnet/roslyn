// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal class MultipleMembersMarginViewModel
    {
        public string ToolTip { get; }

        public ImageMoniker ImageMoniker { get; }

        public ImmutableArray<MemberDisplayViewModel> Members { get; }

        public MultipleMembersMarginViewModel(InheritanceMarginTag tag)
        {
            // TODO: Move this to resources.
            ToolTip = "Click to select member";
            ImageMoniker = tag.Moniker;
            Members = tag.MembersOnLine.SelectAsArray(member => new MemberDisplayViewModel(member));
        }
    }
}
