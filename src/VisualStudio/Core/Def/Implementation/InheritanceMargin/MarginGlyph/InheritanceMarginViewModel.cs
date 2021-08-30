// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        public ImmutableArray<InheritanceMenuItemViewModel> MenuItemViewModels { get; }

        /// <summary>
        /// Scale factor for the margin.
        /// </summary>
        public double ScaleFactor { get; }

        // Internal for testing purpose
        internal InheritanceMarginViewModel(
            ImageMoniker imageMoniker,
            TextBlock toolTipTextBlock,
            string automationName,
            double scaleFactor,
            ImmutableArray<InheritanceMenuItemViewModel> menuItemViewModels)
        {
            ImageMoniker = imageMoniker;
            ToolTipTextBlock = toolTipTextBlock;
            AutomationName = automationName;
            MenuItemViewModels = menuItemViewModels;
            ScaleFactor = scaleFactor;
        }

        public static InheritanceMarginViewModel Create(
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMap classificationFormatMap,
            InheritanceMarginTag tag,
            double zoomLevel)
        {
            var members = tag.MembersOnLine;

            // ZoomLevel is 100 based. (e.g. 150%, 100%)
            // ScaleFactor is 1 based. (e.g. 1.5, 1)
            var scaleFactor = zoomLevel / 100;
            if (members.Length == 1)
            {
                var member = tag.MembersOnLine[0];
                var menuItemViewModels = InheritanceMarginHelpers.CreateMenuItemViewModelsForSingleMember(member.TargetItems);
                var (tooltipTextBlock, automationName) = InheritanceMarginHelpers.CreateToolTipForSingleMember(classificationTypeMap, classificationFormatMap, member);
                return new InheritanceMarginViewModel(tag.Moniker, tooltipTextBlock, automationName, scaleFactor, menuItemViewModels);
            }
            else
            {
                var textBlock = new TextBlock
                {
                    Text = ServicesVSResources.Multiple_members_are_inherited
                };

                // Same automation name can't be set for control for accessibility purpose. So add the line number info.
                var automationName = string.Format(ServicesVSResources.Multiple_members_are_inherited_on_line_0, tag.LineNumber);
                var menuItemViewModels = InheritanceMarginHelpers.CreateMenuItemViewModelsForMultipleMembers(tag.MembersOnLine);
                return new InheritanceMarginViewModel(tag.Moniker, textBlock, automationName, scaleFactor, menuItemViewModels);
            }
        }
    }
}
