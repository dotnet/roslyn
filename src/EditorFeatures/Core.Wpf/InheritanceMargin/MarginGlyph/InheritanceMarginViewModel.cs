// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal class InheritanceMarginViewModel
    {
        /// <summary>
        /// ImageMoniker used for the margin.
        /// </summary>
        public ImageMoniker ImageMoniker { get; }

        /// <summary>
        /// Tooltip for the margin.
        /// </summary>
        public TextBlock ToolTipTextBlock { get; }

        /// <summary>
        /// Text used for automation.
        /// </summary>
        public string AutomationName { get; }

        /// <summary>
        /// ViewModels for the context menu items.
        /// </summary>
        public ImmutableArray<InheritanceContextMenuItemViewModel> MenuItemViewModels { get; }

        public bool HasMultipleMembers { get; }

        private InheritanceMarginViewModel(
            ImageMoniker imageMoniker,
            TextBlock toolTipTextBlock,
            string automationName,
            ImmutableArray<InheritanceContextMenuItemViewModel> menuItemViewModels,
            bool hasMultipleMembers)
        {
            ImageMoniker = imageMoniker;
            ToolTipTextBlock = toolTipTextBlock;
            AutomationName = automationName;
            MenuItemViewModels = menuItemViewModels;
            HasMultipleMembers = hasMultipleMembers;
        }

        public static InheritanceMarginViewModel Create(
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMap classificationFormatMap,
            InheritanceMarginTag tag)
        {
            var members = tag.MembersOnLine;
            if (members.Length == 1)
            {
                var member = tag.MembersOnLine[0];
                var textAppended = " " + EditorFeaturesWpfResources.is_inherited;
                var inlines = member.DisplayTexts.ToInlines(classificationFormatMap, classificationTypeMap);
                WrapMemberWithinApostrophe(inlines);
                inlines.Add(new Run(textAppended));
                var toolTipTextBlock = inlines.ToTextBlock(classificationFormatMap);

                var automationName = member.DisplayTexts.JoinText() + textAppended;
                var menuItemViewModels = member.TargetItems
                    .SelectAsArray(TargetMenuItemViewModel.Create).CastArray<InheritanceContextMenuItemViewModel>();
                return new InheritanceMarginViewModel(tag.Moniker, toolTipTextBlock, automationName, menuItemViewModels, false);
            }
            else
            {
                var textBlock = new TextBlock
                {
                    Text = EditorFeaturesWpfResources.Multiple_members_are_inherited
                };

                // Same automation name can't be set for control. So add the line number info.
                var automationName = string.Format(EditorFeaturesWpfResources.Multiple_members_are_inherited_on_line_0, tag.LineNumber);
                var menuItemViewModels = tag.MembersOnLine
                    .SelectAsArray(MemberMenuItemViewModel.Create)
                    .CastArray<InheritanceContextMenuItemViewModel>();
                return new InheritanceMarginViewModel(tag.Moniker, textBlock, automationName, menuItemViewModels, true);
            }
        }

        private static void WrapMemberWithinApostrophe(IList<Inline> memberInlines)
        {
            memberInlines.Insert(0, new Run("'"));
            memberInlines.Add(new Run("'"));
        }
    }
}
