// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
{
    /// <summary>
    /// View model used to display a member in MenuItem. Only used when there are multiple members on the same line.
    /// </summary>
    internal class MemberMenuItemViewModel : InheritanceMenuItemViewModel
    {
        /// <summary>
        /// Inheritance Targets for this member.
        /// </summary>
        public ImmutableArray<MenuItemViewModel> Targets { get; }

        public MemberMenuItemViewModel(
            string displayContent,
            ImageMoniker imageMoniker,
            string automationName,
            ImmutableArray<MenuItemViewModel> targets) : base(displayContent, imageMoniker, automationName)
        {
            Targets = targets;
        }

        public static MemberMenuItemViewModel CreateWithNoSeparator(InheritanceMarginItem member)
        {
            var displayName = member.DisplayTexts.JoinText();
            return new MemberMenuItemViewModel(
                displayName,
                member.Glyph.GetImageMoniker(),
                displayName,
                member.TargetItems.SelectAsArray(TargetMenuItemViewModel.Create).CastArray<MenuItemViewModel>());
        }

        public static MemberMenuItemViewModel CreateWithSeparator(InheritanceMarginItem member)
        {
            var displayName = member.DisplayTexts.JoinText();
            return new MemberMenuItemViewModel(
                displayName,
                member.Glyph.GetImageMoniker(),
                displayName,
                member.TargetItems.GroupBy(target => target.RelationToMember)
                    .Select((grouping, index) => InheritanceMarginHelpers.CreateMenuItemsWithSeparator(grouping.SelectAsArray(g => g), grouping.Key, index == 0))
                    .ToImmutableArray()
                    .CastArray<MenuItemViewModel>());
        }
    }
}
