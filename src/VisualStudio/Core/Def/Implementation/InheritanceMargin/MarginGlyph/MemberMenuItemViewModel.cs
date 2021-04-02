// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
{
    /// <summary>
    /// View model used to display a member in MenuItem. Only used when there are multiple members on the same line.
    /// </summary>
    internal class MemberMenuItemViewModel : InheritanceContextMenuItemViewModel
    {
        /// <summary>
        /// Inheritance Targets for this member.
        /// </summary>
        public ImmutableArray<TargetMenuItemViewModel> Targets { get; }

        public MemberMenuItemViewModel(
            string displayContent,
            ImageMoniker imageMoniker,
            string automationName,
            ImmutableArray<TargetMenuItemViewModel> targets) : base(displayContent, imageMoniker, automationName)
        {
            Targets = targets;
        }

        public static MemberMenuItemViewModel Create(InheritanceMarginItem member)
        {
            var displayName = member.DisplayTexts.JoinText();
            return new MemberMenuItemViewModel(
                displayName,
                member.Glyph.GetImageMoniker(),
                displayName,
                member.TargetItems.SelectAsArray(TargetMenuItemViewModel.Create));
        }
    }
}
