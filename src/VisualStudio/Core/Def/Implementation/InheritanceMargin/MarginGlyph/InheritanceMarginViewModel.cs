﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
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

                // Here we want to show a classified text with loc text,
                // e.g. 'Bar' is inherited.
                // But the classified text are inlines, so can't directly use string.format to generate the string
                var inlines = member.DisplayTexts.ToInlines(classificationFormatMap, classificationTypeMap);
                var startOfThePlaceholder = ServicesVSResources._0_is_inherited.IndexOf("{0}", StringComparison.Ordinal);
                var prefixString = ServicesVSResources._0_is_inherited[..startOfThePlaceholder];
                var suffixString = ServicesVSResources._0_is_inherited[(startOfThePlaceholder + "{0}".Length)..];
                inlines.Insert(0, new Run(prefixString));
                inlines.Add(new Run(suffixString));
                var toolTipTextBlock = inlines.ToTextBlock(classificationFormatMap);
                toolTipTextBlock.FlowDirection = FlowDirection.LeftToRight;

                var automationName = string.Format(ServicesVSResources._0_is_inherited, member.DisplayTexts.JoinText());
                var menuItemViewModels = member.TargetItems
                    .SelectAsArray(TargetMenuItemViewModel.Create).CastArray<InheritanceContextMenuItemViewModel>();
                return new InheritanceMarginViewModel(tag.Moniker, toolTipTextBlock, automationName, menuItemViewModels, false);
            }
            else
            {
                var textBlock = new TextBlock
                {
                    Text = ServicesVSResources.Multiple_members_are_inherited
                };

                // Same automation name can't be set for control. So add the line number info.
                var automationName = string.Format(ServicesVSResources.Multiple_members_are_inherited_on_line_0, tag.LineNumber);
                var menuItemViewModels = tag.MembersOnLine
                    .SelectAsArray(MemberMenuItemViewModel.Create)
                    .CastArray<InheritanceContextMenuItemViewModel>();
                return new InheritanceMarginViewModel(tag.Moniker, textBlock, automationName, menuItemViewModels, true);
            }
        }
    }
}
