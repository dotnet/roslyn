// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal class SingleMemberMarginViewModel
    {
        public string ToolTip { get; }

        public ImageMoniker ImageMoniker { get; }

        public ImmutableArray<TargetDisplayViewModel> Targets { get; }

        public SingleMemberMarginViewModel(InheritanceMarginTag tag)
        {
            // TODO: Move this to resources.
            ToolTip = "Click to select target";
            ImageMoniker = tag.Moniker;
            var member = tag.MembersOnLine[0];
            var targets = member.TargetItems;
            Targets = targets.SelectAsArray(item => new TargetDisplayViewModel(item));
        }
    }
}
