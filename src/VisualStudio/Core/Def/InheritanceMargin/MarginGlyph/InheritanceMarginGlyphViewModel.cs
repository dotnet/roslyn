// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;

internal sealed class InheritanceMarginGlyphViewModel
{
    private readonly ClassificationTypeMap _classificationTypeMap;
    private readonly IClassificationFormatMap _classificationFormatMap;
    private readonly InheritanceMarginTag _tag;

    /// <summary>
    /// ImageMoniker used for the margin.
    /// </summary>
    public ImageMoniker ImageMoniker => _tag.Moniker;

    /// <summary>
    /// Tooltip for the margin.
    /// </summary>
    public TextBlock ToolTipTextBlock
    {
        get
        {
            var members = _tag.MembersOnLine;
            TextBlock textBlock;

            if (members.All(static m => m.TopLevelDisplayText != null))
            {
                var member = _tag.MembersOnLine[0];
                Contract.ThrowIfNull(member.TopLevelDisplayText);

                textBlock = new[] { new TaggedText(TextTags.Text, member.TopLevelDisplayText) }.ToTextBlock(_classificationFormatMap, _classificationTypeMap);
            }
            else if (members.Length == 1)
            {
                var member = _tag.MembersOnLine[0];

                // Preserve symbol classification within the localized tooltip.
                var inlines = member.DisplayTexts.ToInlines(_classificationFormatMap, _classificationTypeMap);
                var format = GetToolTipFormat(member);
                var startOfThePlaceholder = format.IndexOf("{0}", StringComparison.Ordinal);
                var prefixString = format[..startOfThePlaceholder];
                var suffixString = format[(startOfThePlaceholder + "{0}".Length)..];
                inlines.Insert(0, new Run(prefixString));
                inlines.Add(new Run(suffixString));

                textBlock = inlines.ToTextBlock(_classificationFormatMap);
                textBlock.FlowDirection = FlowDirection.LeftToRight;
            }
            else
            {
                textBlock = new TextBlock
                {
                    Text = ServicesVSResources.Show_inheritance_for_members_on_this_line
                };
            }
            textBlock.LayoutTransform = new ScaleTransform(ScaleFactor, ScaleFactor);
            textBlock.UseLayoutRounding = true;
            RenderOptions.SetClearTypeHint(textBlock, ClearTypeHint.Enabled);
            return textBlock;
        }
    }

    /// <summary>
    /// Text used for automation.
    /// </summary>
    public string AutomationName { get; }

    /// <summary>
    /// ViewModels for the context menu items.
    /// </summary>
    public ImmutableArray<MenuItemViewModel> MenuItemViewModels { get; }

    /// <summary>
    /// Scale factor for the margin.
    /// </summary>
    public double ScaleFactor { get; }

    // Placement offsets inherit the target's zoom, unlike the tooltip's chrome.
    public double ToolTipVerticalOffset => Microsoft.VisualStudio.Shell.Controls.ToolTip.Offset / ScaleFactor;

    private InheritanceMarginGlyphViewModel(
        InheritanceMarginTag tag,
        ClassificationTypeMap classificationTypeMap,
        IClassificationFormatMap classificationFormatMap,
        string automationName,
        double scaleFactor,
        ImmutableArray<MenuItemViewModel> menuItemViewModels)
    {
        _classificationTypeMap = classificationTypeMap;
        _classificationFormatMap = classificationFormatMap;
        _tag = tag;
        AutomationName = automationName;
        MenuItemViewModels = menuItemViewModels;
        ScaleFactor = scaleFactor;
    }

    public static InheritanceMarginGlyphViewModel Create(
        ClassificationTypeMap classificationTypeMap,
        IClassificationFormatMap classificationFormatMap,
        InheritanceMarginTag tag,
        double zoomLevel)
    {
        var members = tag.MembersOnLine;

        // ZoomLevel is 100 based. (e.g. 150%, 100%)
        // ScaleFactor is 1 based. (e.g. 1.5, 1)
        var scaleFactor = zoomLevel / 100;
        if (members.All(m => m.TopLevelDisplayText != null))
        {
            var member = tag.MembersOnLine[0];
            var automationName = member.TopLevelDisplayText;

            var menuItemViewModels = members.SelectManyAsArray(m => InheritanceMarginHelpers.CreateModelsForMarginItem(m));
            return new InheritanceMarginGlyphViewModel(tag, classificationTypeMap, classificationFormatMap, automationName!, scaleFactor, menuItemViewModels);
        }
        else if (members.Length == 1)
        {
            var member = tag.MembersOnLine[0];

            var automationName = string.Format(GetToolTipFormat(member), member.DisplayTexts.JoinText());
            var menuItemViewModels = InheritanceMarginHelpers.CreateModelsForMarginItem(member);
            return new InheritanceMarginGlyphViewModel(tag, classificationTypeMap, classificationFormatMap, automationName, scaleFactor, menuItemViewModels);
        }
        else
        {
            // Same automation name can't be set for control for accessibility purpose. So add the line number info.
            var automationName = string.Format(ServicesVSResources.Show_inheritance_for_members_on_line_0, tag.LineNumber);
            var menuItemViewModels = InheritanceMarginHelpers.CreateMenuItemViewModelsForMultipleMembers(tag.MembersOnLine);
            return new InheritanceMarginGlyphViewModel(tag, classificationTypeMap, classificationFormatMap, automationName, scaleFactor, menuItemViewModels);
        }
    }

    private static string GetToolTipFormat(InheritanceMarginItem member)
    {
        var relationships = InheritanceRelationship.None;
        foreach (var target in member.TargetItems)
            relationships |= target.RelationToMember;

        return relationships switch
        {
            InheritanceRelationship.BaseType => ServicesVSResources.Show_base_types_for_0,
            InheritanceRelationship.DerivedType => ServicesVSResources.Show_derived_types_for_0,
            InheritanceRelationship.ImplementedInterface => ServicesVSResources.Show_implemented_interfaces_for_0,
            InheritanceRelationship.InheritedInterface => ServicesVSResources.Show_base_interfaces_for_0,
            InheritanceRelationship.ImplementingType or InheritanceRelationship.ImplementingMember => ServicesVSResources.Show_implementations_for_0,
            InheritanceRelationship.ImplementedMember => ServicesVSResources.Show_implemented_members_for_0,
            InheritanceRelationship.OverriddenMember => ServicesVSResources.Show_base_members_for_0,
            InheritanceRelationship.OverridingMember => ServicesVSResources.Show_overrides_for_0,
            InheritanceRelationship.BaseType | InheritanceRelationship.ImplementedInterface
                => ServicesVSResources.Show_base_types_and_implemented_interfaces_for_0,
            InheritanceRelationship.BaseType | InheritanceRelationship.DerivedType
                => ServicesVSResources.Show_base_and_derived_types_for_0,
            InheritanceRelationship.ImplementedMember | InheritanceRelationship.OverriddenMember
                => ServicesVSResources.Show_implemented_and_overridden_members_for_0,
            InheritanceRelationship.OverriddenMember | InheritanceRelationship.OverridingMember
                => ServicesVSResources.Show_base_members_and_overrides_for_0,
            _ => ServicesVSResources.Show_inheritance_for_0,
        };
    }
}
