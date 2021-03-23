// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal class SingleMemberMarginViewModel
    {
        public ImageMoniker Moniker { get; }

        public string ToolTip { get; }

        public ImmutableArray<TargetDisplayRowViewModel> Targets { get; }

        public SingleMemberMarginViewModel(InheritanceMarginTag tag)
        {
            Moniker = tag.Moniker;
            ToolTip = GetToolTip(tag);
            Targets = tag.MembersOnLine[0].TargetItems.SelectAsArray(item => new TargetDisplayRowViewModel(item));
        }

        // TODO: Revisited the tooltip
        private static string GetToolTip(InheritanceMarginTag tag)
        {
            var members = tag.MembersOnLine;
            if (members.Length == 1)
            {
                var member = members[0];
                var targets = member.TargetItems;
                return targets.Length == 1 ? "Click to navigate to the target" : "Click to select target";
            }
            else
            {
                return "Click to select member";
            }
        }
    }
}
