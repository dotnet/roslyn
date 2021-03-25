// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    /// <summary>
    /// View model used when there is only one member on the line to show the margin.
    /// </summary>
    internal class SingleMemberMarginViewModel
    {
        /// <summary>
        /// ImageMoniker used for the margin.
        /// </summary>
        public ImageMoniker ImageMoniker { get; }

        /// <summary>
        /// Tooltip for the margin
        /// </summary>
        public string ToolTip { get; }

        /// <summary>
        /// Text used for automation.
        /// </summary>
        public string AutomationName { get; }

        public ImmutableArray<TargetDisplayViewModel> Targets { get; }

        public SingleMemberMarginViewModel(InheritanceMarginTag tag)
        {
            ImageMoniker = tag.Moniker;
            var member = tag.MembersOnLine[0];
            var memberDisplayName = member.DisplayName;
            ToolTip = string.Format(EditorFeaturesWpfResources.Click_to_view_all_inheritance_targets_for_0, memberDisplayName);
            AutomationName = ToolTip;
            var targets = member.TargetItems;
            Targets = targets.SelectAsArray(item => new TargetDisplayViewModel(item));
        }
    }
}
