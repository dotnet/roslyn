// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
{
    internal static class InheritanceMarginHelpers
    {
        private static readonly ImmutableArray<InheritanceRelationship> s_relationships_Shown_As_I_Up_Arrow
            = ImmutableArray.Create(
                InheritanceRelationship.ImplementedInterface,
                InheritanceRelationship.InheritedInterface,
                InheritanceRelationship.ImplementedMember);

        private static readonly ImmutableArray<InheritanceRelationship> s_relationships_Shown_As_I_Down_Arrow
            = ImmutableArray.Create(
                InheritanceRelationship.ImplementingType,
                InheritanceRelationship.ImplementingMember);

        private static readonly ImmutableArray<InheritanceRelationship> s_relationships_Shown_As_O_Up_Arrow
            = ImmutableArray.Create(
                InheritanceRelationship.BaseType,
                InheritanceRelationship.OverriddenMember);

        private static readonly ImmutableArray<InheritanceRelationship> s_relationships_Shown_As_O_Down_Arrow
            = ImmutableArray.Create(InheritanceRelationship.DerivedType, InheritanceRelationship.OverridingMember);

        /// <summary>
        /// Decide which moniker should be shown.
        /// </summary>
        public static ImageMoniker GetMoniker(InheritanceRelationship inheritanceRelationship)
        {
            //  If there are multiple targets and we have the corresponding compound image, use it
            if (s_relationships_Shown_As_I_Up_Arrow.Any(flag => inheritanceRelationship.HasFlag(flag))
                && s_relationships_Shown_As_O_Down_Arrow.Any(flag => inheritanceRelationship.HasFlag(flag)))
            {
                return KnownMonikers.ImplementingOverridden;
            }

            if (s_relationships_Shown_As_I_Up_Arrow.Any(flag => inheritanceRelationship.HasFlag(flag))
                && s_relationships_Shown_As_O_Up_Arrow.Any(flag => inheritanceRelationship.HasFlag(flag)))
            {
                return KnownMonikers.ImplementingOverriding;
            }

            // Otherwise, show the image based on this preference
            if (s_relationships_Shown_As_I_Up_Arrow.Any(flag => inheritanceRelationship.HasFlag(flag)))
            {
                return KnownMonikers.Implementing;
            }

            if (s_relationships_Shown_As_I_Down_Arrow.Any(flag => inheritanceRelationship.HasFlag(flag)))
            {
                return KnownMonikers.Implemented;
            }

            if (s_relationships_Shown_As_O_Up_Arrow.Any(flag => inheritanceRelationship.HasFlag(flag)))
            {
                return KnownMonikers.Overriding;
            }

            if (s_relationships_Shown_As_O_Down_Arrow.Any(flag => inheritanceRelationship.HasFlag(flag)))
            {
                return KnownMonikers.Overridden;
            }

            // The relationship is None. Don't know what image should be shown, throws
            throw ExceptionUtilities.UnexpectedValue(inheritanceRelationship);
        }

        /// <summary>
        /// Create the MenuItemViewModel for the <paramref name="targets"/>.
        /// Note: The order of headers are sorted based on the value of <see cref="InheritanceRelationship"/> to matches the
        /// localized tooltip.
        /// </summary>
        public static ImmutableArray<MenuItemViewModel> CreateMenuItemViewModelsForSingleMember(ImmutableArray<InheritanceTargetItem> targets)
            => targets.OrderBy(target => target.DisplayTaggedTexts.JoinText())
                .GroupBy(target => target.RelationToMember)
                .OrderBy(grouping => grouping.Key)
                .SelectMany(grouping => CreateMenuItemsWithHeader(grouping.Key, grouping))
                .ToImmutableArray();

        /// <summary>
        /// Create the view models for the inheritance targets of multiple members
        /// e.g.
        /// MemberViewModel1 -> HeaderViewModel
        ///                     Target1ViewModel
        ///                     HeaderViewModel
        ///                     Target2ViewModel
        /// MemberViewModel2 -> HeaderViewModel
        ///                     Target4ViewModel
        ///                     HeaderViewModel
        ///                     Target5ViewModel
        /// </summary>
        public static ImmutableArray<MenuItemViewModel> CreateMenuItemViewModelsForMultipleMembers(ImmutableArray<InheritanceMarginItem> members)
        {
            Contract.ThrowIfTrue(members.Length <= 1);
            return members.SelectAsArray(MemberMenuItemViewModel.CreateWithHeaderInTargets).CastArray<MenuItemViewModel>();
        }

        public static ImmutableArray<MenuItemViewModel> CreateMenuItemsWithHeader(
            InheritanceRelationship relationship,
            IEnumerable<InheritanceTargetItem> targets)
        {
            using var _ = CodeAnalysis.PooledObjects.ArrayBuilder<MenuItemViewModel>.GetInstance(out var builder);
            var displayContent = relationship switch
            {
                InheritanceRelationship.ImplementedInterface => ServicesVSResources.Implemented_interfaces,
                InheritanceRelationship.BaseType => ServicesVSResources.Base_Types,
                InheritanceRelationship.DerivedType => ServicesVSResources.Derived_types,
                InheritanceRelationship.InheritedInterface => ServicesVSResources.Inherited_interfaces,
                InheritanceRelationship.ImplementingType => ServicesVSResources.Implementing_types,
                InheritanceRelationship.ImplementedMember => ServicesVSResources.Implemented_members,
                InheritanceRelationship.OverriddenMember => ServicesVSResources.Overridden_members,
                InheritanceRelationship.OverridingMember => ServicesVSResources.Overriding_members,
                InheritanceRelationship.ImplementingMember => ServicesVSResources.Implementing_members,
                _ => throw ExceptionUtilities.UnexpectedValue(relationship)
            };

            var headerViewModel = new HeaderMenuItemViewModel(displayContent, GetMoniker(relationship), displayContent);
            builder.Add(headerViewModel);
            foreach (var targetItem in targets)
            {
                builder.Add(TargetMenuItemViewModel.Create(targetItem));
            }

            return builder.ToImmutable();
        }

        private static string GetToolTipTemplateForSingleTarget(InheritanceRelationship headerRelationship)
            => headerRelationship switch
            {
                InheritanceRelationship.ImplementedInterface => ServicesVSResources._0_implements_1,
                InheritanceRelationship.BaseType => ServicesVSResources._0_is_derived_from_1,
                InheritanceRelationship.DerivedType => ServicesVSResources._0_derives_1,
                InheritanceRelationship.InheritedInterface => ServicesVSResources._0_is_inherited_from_1,
                InheritanceRelationship.ImplementingType => ServicesVSResources._0_is_implemented_by_1,
                InheritanceRelationship.ImplementedMember => ServicesVSResources._0_implements_1,
                InheritanceRelationship.ImplementingMember => ServicesVSResources._0_is_implemented_by_1,
                InheritanceRelationship.OverriddenMember => ServicesVSResources._0_overrides_1,
                InheritanceRelationship.OverridingMember => ServicesVSResources._0_is_overridden_by_1,
                _ => throw ExceptionUtilities.UnexpectedValue(headerRelationship),
            };

        private static string GetToolTipTemplateForMultipleTargetsUnderSameHeader(InheritanceRelationship headerRelationship)
            => headerRelationship switch
            {
                InheritanceRelationship.ImplementedInterface => ServicesVSResources._0_has_multiple_implemented_interfaces,
                InheritanceRelationship.BaseType => ServicesVSResources._0_has_multiple_base_types,
                InheritanceRelationship.DerivedType => ServicesVSResources._0_has_multiple_derived_types,
                InheritanceRelationship.InheritedInterface => ServicesVSResources._0_has_multiple_inherited_interfaces,
                InheritanceRelationship.ImplementingType => ServicesVSResources._0_has_multiple_implementing_types,
                InheritanceRelationship.ImplementedMember => ServicesVSResources._0_implements_members_from_multiple_interfaces,
                InheritanceRelationship.ImplementingMember => ServicesVSResources._0_is_implemented_by_members_from_multiple_types,
                InheritanceRelationship.OverriddenMember => ServicesVSResources._0_overrides_members_from_multiple_classes,
                InheritanceRelationship.OverridingMember => ServicesVSResources._0_is_overridden_by_members_from_multiple_classes,
                _ => throw ExceptionUtilities.UnexpectedValue(headerRelationship)
            };

        private static string GetToolTipContentForMultipleHeaders(InheritanceRelationship aggregateRelationship)
        {
            // For class/struct,
            // 'class Bar' has Implemented interfaces, Base types and Derived types
            if (aggregateRelationship.HasFlag(InheritanceRelationship.ImplementedInterface | InheritanceRelationship.BaseType | InheritanceRelationship.DerivedType))
            {
                return ServicesVSResources._0_has_implemented_interfaces_base_types_and_derived_types;
            }

            // For class/struct
            // 'class Bar' has Implemented interfaces and Base types
            if (aggregateRelationship.HasFlag(InheritanceRelationship.ImplementedInterface | InheritanceRelationship.BaseType))
            {
                return ServicesVSResources._0_has_implemented_interfaces_and_base_types;
            }

            // For class/struct
            // 'class Bar' has Base types and Derived types
            if (aggregateRelationship.HasFlag(InheritanceRelationship.BaseType | InheritanceRelationship.DerivedType))
            {
                return ServicesVSResources._0_has_base_types_and_derived_types;
            }

            // For class/struct
            // 'class Bar' has Implemented interfaces and Derived types
            if (aggregateRelationship.HasFlag(InheritanceRelationship.ImplementedInterface | InheritanceRelationship.DerivedType))
            {
                return ServicesVSResources._0_has_implemented_interfaces_and_derived_types;
            }

            // For interface
            // 'interface IBar' has inherited interfaces and Implementing types
            if (aggregateRelationship.HasFlag(InheritanceRelationship.InheritedInterface | InheritanceRelationship.ImplementingType))
            {
                return ServicesVSResources._0_has_inherited_interfaces_and_implementing_types;
            }

            // For member of class/struct
            // 'Bar()' has Implemented members, Overridden members and Overriding members.
            if (aggregateRelationship.HasFlag(InheritanceRelationship.ImplementedMember | InheritanceRelationship.OverriddenMember | InheritanceRelationship.OverridingMember))
            {
                return ServicesVSResources._0_has_implemented_members_overridden_members_and_overriding_members;
            }

            // For member of class/struct
            // 'Bar()' has Implemented members and Overridden members.
            if (aggregateRelationship.HasFlag(InheritanceRelationship.ImplementedMember | InheritanceRelationship.OverriddenMember))
            {
                return ServicesVSResources._0_has_implemented_members_and_overridden_members;
            }

            // For member of class/struct
            // 'Bar()' has Implemented members and Overriding members.
            if (aggregateRelationship.HasFlag(InheritanceRelationship.ImplementedMember | InheritanceRelationship.OverridingMember))
            {
                return ServicesVSResources._0_has_implemented_members_and_overriding_members;
            }

            // For member of class/struct
            // 'Bar()' has Overridden members and Overriding members.
            if (aggregateRelationship.HasFlag(InheritanceRelationship.OverriddenMember | InheritanceRelationship.OverridingMember))
            {
                return ServicesVSResources._0_has_overridden_members_and_overriding_members;
            }

            throw ExceptionUtilities.UnexpectedValue(aggregateRelationship);
        }

        /// <summary>
        /// Create the TextBlock used as the tooltip of the glyph. (The texts of the textBlock is colorized)
        /// Also return the content of the text block.
        /// </summary>
        public static (TextBlock tooltipTextBlock, string tooltipText) CreateToolTipForSingleMember(
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMap classificationFormatMap,
            InheritanceMarginItem member)
        {
            var targets = member.TargetItems;
            Contract.ThrowIfTrue(targets.IsEmpty);
            if (targets.Length == 1)
            {
                var target = targets[0];
                var contentTemplate = GetToolTipTemplateForSingleTarget(target.RelationToMember);
                var tooltipTextBlock = FormatTaggedText(classificationTypeMap, classificationFormatMap, contentTemplate, member.DisplayTaggedTexts, target.DisplayTaggedTexts);
                var automationName = string.Format(contentTemplate, member.DisplayTaggedTexts.JoinText(), target.DisplayTaggedTexts.JoinText());
                return (tooltipTextBlock, automationName);
            }

            using var _ = PooledHashSet<InheritanceRelationship>.GetInstance(out var targetRelationshipSet);
            foreach (var target in targets)
                targetRelationshipSet.Add(target.RelationToMember);

            if (targetRelationshipSet.Count == 1)
            {
                var relationship = targets[0].RelationToMember;
                var contentTemplate = GetToolTipTemplateForMultipleTargetsUnderSameHeader(relationship);
                var toolTipTextBlock = FormatTaggedText(classificationTypeMap, classificationFormatMap, contentTemplate, member.DisplayTaggedTexts);
                var automationName = string.Format(contentTemplate, member.DisplayTaggedTexts.JoinText());
                return (toolTipTextBlock, automationName);
            }
            else
            {
                var aggregateRelationship = GetAggregateRelationship(targetRelationshipSet);
                var contentTemplate = GetToolTipContentForMultipleHeaders(aggregateRelationship);
                var toolTipTextBlock = FormatTaggedText(classificationTypeMap, classificationFormatMap, contentTemplate, member.DisplayTaggedTexts);
                var automationName = string.Format(contentTemplate, member.DisplayTaggedTexts.JoinText());
                return (toolTipTextBlock, automationName);
            }
        }

        private static InheritanceRelationship GetAggregateRelationship(HashSet<InheritanceRelationship> relationships)
        {
            Contract.ThrowIfTrue(relationships.Count == 0);
            var result = relationships.First();
            foreach (var relationship in relationships.Skip(1))
            {
                result |= relationship;
            }

            return result;
        }

        /// <summary>
        /// Format the <paramref name="contentTemplate"/> using the <paramref name="taggedTexts"/>. And return the formatted text as a TextBlock.
        /// <paramref name="contentTemplate"/> is a localized text with placeholders in it.
        /// e.g. '{0}' is inherited. '{0}' implements '{1}'.
        /// <paramref name="taggedTexts"/> is a array of tagged texts used to generate colorized text.
        /// </summary>
        private static TextBlock FormatTaggedText(
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMap classificationFormatMap,
            string contentTemplate,
            params ImmutableArray<TaggedText>[] taggedTexts)
        {
            Contract.ThrowIfTrue(taggedTexts.Length == 0);
            using var _1 = ArrayBuilder<Inline>.GetInstance(out var inlinesBuilder);
            using var _2 = ArrayBuilder<int>.GetInstance(out var indexBuilder);

            // 1. Find all the indexes of the placeholders from the template.
            for (var i = 0; i < taggedTexts.Length; i++)
            {
                indexBuilder.Add(contentTemplate.IndexOf($"{{{i}}}", StringComparison.Ordinal));
            }

            var indexArray = indexBuilder.ToImmutable();
            Contract.ThrowIfTrue(taggedTexts.Length != indexArray.Length);
            var lengthOfThePlaceholder = "{0}".Length;

            // 2. Spilt the template and insert Inline to the builder
            for (var i = 0; i < indexArray.Length; i++)
            {
                // Have two index point to the start of current placeholder and the end of the previous placeholder
                // ".....{0}.....{1}....{2}...."
                //         ↑     ↑
                //    prevIndex  currentIndex
                // to insert the string between {0} and {1}
                var endOfPreviousPlaceHolder = i == 0 ? 0 : indexArray[i - 1] + lengthOfThePlaceholder;
                var currentIndex = indexArray[i];
                var prefixString = contentTemplate[endOfPreviousPlaceHolder..currentIndex];
                inlinesBuilder.Add(new Run(prefixString));

                // Add the TaggedTexts
                var currentTaggedText = taggedTexts[i];
                inlinesBuilder.AddRange(currentTaggedText.ToInlines(classificationFormatMap, classificationTypeMap));

                // If this is the last placeholder, try to see if we need to append the suffix string to the end
                // ".....{0}.....{1}....{2}...."
                //                      ↑
                //                      currentIndex
                if (i == indexArray.Length - 1 && currentIndex + lengthOfThePlaceholder < contentTemplate.Length)
                {
                    var suffixString = contentTemplate[(currentIndex + lengthOfThePlaceholder)..];
                    if (!string.IsNullOrEmpty(suffixString))
                    {
                        inlinesBuilder.Add(new Run(suffixString));
                    }
                }
            }

            // 3. Generate the textBlock by using the generated inlines.
            // This textBlock will later be used to shown as the tooltip content of inheritance margin glyph
            var textBlock = inlinesBuilder.ToTextBlock(classificationFormatMap);
            textBlock.FlowDirection = FlowDirection.LeftToRight;
            return textBlock;
        }
    }
}
