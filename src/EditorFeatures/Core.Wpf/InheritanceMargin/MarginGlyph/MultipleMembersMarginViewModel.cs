// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    /// <summary>
    /// View model used to show multiple members on the same line.
    /// </summary>
    internal class MultipleMembersMarginViewModel
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

        /// <summary>
        /// All the members on this line.
        /// </summary>
        public ImmutableArray<MemberDisplayViewModel> Members { get; }

        public MultipleMembersMarginViewModel(InheritanceMarginTag tag)
        {
            ToolTip = EditorFeaturesWpfResources.Click_to_select_member;
            AutomationName = ToolTip;
            ImageMoniker = tag.Moniker;
            Members = tag.MembersOnLine.SelectAsArray(member => new MemberDisplayViewModel(member));
        }
    }
}
