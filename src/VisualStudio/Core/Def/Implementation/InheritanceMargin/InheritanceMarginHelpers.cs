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

        public static ImmutableArray<InheritanceMenuItemViewModel> CreateMenuItemViewModelsForSingleMember(ImmutableArray<InheritanceTargetItem> targets)
            => targets.OrderBy(target => target.DisplayName)
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
        public static ImmutableArray<InheritanceMenuItemViewModel> CreateMenuItemViewModelsForMultipleMembers(ImmutableArray<InheritanceMarginItem> members)
        {
            Contract.ThrowIfTrue(members.Length <= 1);
            // For multiple members, check if all the targets have the same inheritance relationship.
            // If so, then don't add the header, because it is already indicated by the margin.
            // Otherwise, add the Header.
            return members.SelectAsArray(MemberMenuItemViewModel.CreateWithHeaderInTargets).CastArray<InheritanceMenuItemViewModel>();
        }

        public static ImmutableArray<InheritanceMenuItemViewModel> CreateMenuItemsWithHeader(
            InheritanceRelationship relationship,
            IEnumerable<InheritanceTargetItem> targets)
        {
            using var _ = CodeAnalysis.PooledObjects.ArrayBuilder<InheritanceMenuItemViewModel>.GetInstance(out var builder);
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
                InheritanceRelationship.BaseType => ServicesVSResources._0_is_derived_from__1,
                InheritanceRelationship.DerivedType => ServicesVSResources._0_derives_1,
                InheritanceRelationship.InheritedInterface => ServicesVSResources._0_is_inherited_from_1,
                InheritanceRelationship.ImplementingType => ServicesVSResources._0_is_implemented_by_1,
                InheritanceRelationship.ImplementedMember => ServicesVSResources._0_implements_1,
                InheritanceRelationship.OverriddenMember => ServicesVSResources._0_overrides_1,
                InheritanceRelationship.OverridingMember => ServicesVSResources._0_is_overridden_by_1,
                InheritanceRelationship.ImplementingMember => ServicesVSResources._0_is_implemented_by_1,
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
                InheritanceRelationship.OverriddenMember => ServicesVSResources._0_overrides_members_from_multiple_classes,
                InheritanceRelationship.OverridingMember => ServicesVSResources._0_is_overridden_by_members_from_multiple_classes,
                InheritanceRelationship.ImplementingMember => ServicesVSResources._0_is_implemented_by_members_from_multiple_types,
                _ => throw ExceptionUtilities.UnexpectedValue(headerRelationship)
            };

        private static string GetToolTipContentForMultipleHeader(InheritanceRelationship aggregateRelationship)
        {
            //
            if (aggregateRelationship.HasFlag(InheritanceRelationship.ImplementedInterface | InheritanceRelationship.BaseType | InheritanceRelationship.DerivedType))
            {
                return ServicesVSResources._0_has_implemented_interfaces_based_types_and_derived_types;
            }

            if (aggregateRelationship.HasFlag(InheritanceRelationship.ImplementedInterface | InheritanceRelationship.BaseType))
            {
                return ServicesVSResources._0_has_implemented_interfaces_and_based_types;
            }

            if (aggregateRelationship.HasFlag(InheritanceRelationship.BaseType | InheritanceRelationship.DerivedType))
            {
                return ServicesVSResources._0_has_base_types_and_derived_types;
            }

            if (aggregateRelationship.HasFlag(InheritanceRelationship.ImplementedInterface | InheritanceRelationship.DerivedType))
            {
                return ServicesVSResources._0_has_implemented_interfaces_and_derived_types;
            }

            //
            if (aggregateRelationship.HasFlag(InheritanceRelationship.InheritedInterface | InheritanceRelationship.ImplementingType))
            {
                return ServicesVSResources._0_has_inherited_interfaces_and_implementing_types;
            }

            //
            if (aggregateRelationship.HasFlag(InheritanceRelationship.ImplementedMember | InheritanceRelationship.OverriddenMember | InheritanceRelationship.OverridingMember))
            {
                return ServicesVSResources._0_has_implemented_members_overridden_members_and_overriding_members;
            }

            if (aggregateRelationship.HasFlag(InheritanceRelationship.ImplementedMember | InheritanceRelationship.OverriddenMember))
            {
                return ServicesVSResources._0_has_implemented_members_and_overridden_members;
            }

            if (aggregateRelationship.HasFlag(InheritanceRelationship.ImplementedMember | InheritanceRelationship.OverridingMember))
            {
                return ServicesVSResources._0_has_implemented_members_and_overriding_members;
            }

            if (aggregateRelationship.HasFlag(InheritanceRelationship.OverriddenMember | InheritanceRelationship.OverridingMember))
            {
                return ServicesVSResources._0_has_overridden_members_and_overriding_members;
            }

            throw ExceptionUtilities.Unreachable;
        }

        public static TextBlock CreateToolTipTextBlockForSingleMember(
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMap classificationFormatMap,
            InheritanceMarginItem member)
        {
            var targets = member.TargetItems;
            Contract.ThrowIfTrue(targets.IsEmpty);
            if (targets.Length == 1)
            {
                var target = targets[0];
                var tooltipTemplate = GetToolTipTemplateForSingleTarget(target.RelationToMember);
                return Format(classificationTypeMap, classificationFormatMap, tooltipTemplate, member.TaggedTexts, target.DefinitionItem.DisplayParts);
            }
            else if (targets.Select(target => target.RelationToMember).IsSingle())
            {
                var relationship = targets[0].RelationToMember;
                var contentTemplate = GetToolTipTemplateForMultipleTargetsUnderSameHeader(relationship);
                return Format(classificationTypeMap, classificationFormatMap, contentTemplate, member.TaggedTexts);
            }
            else
            {
                var aggregateRelationship = GetAggregateRelationship(targets.SelectAsArray(target => target.RelationToMember));
                var contentTemplate = GetToolTipContentForMultipleHeader(aggregateRelationship);
                return Format(classificationTypeMap, classificationFormatMap, contentTemplate, member.TaggedTexts);
            }
        }

        public static InheritanceRelationship GetAggregateRelationship(ImmutableArray<InheritanceRelationship> relationships)
        {
            Contract.ThrowIfTrue(relationships.IsEmpty);
            var result = relationships[0];
            foreach (var relationship in relationships.Skip(1))
            {
                result |= relationship;
            }

            return result;
        }

        private static TextBlock Format(
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMap classificationFormatMap,
            string contentTemplate,
            ImmutableArray<TaggedText> memberTaggedText)
        {
            var allInlines = new List<Inline>();
            var startOfTheFirstPlaceholder = contentTemplate.IndexOf("{0}", StringComparison.Ordinal);
            var prefixString = contentTemplate[..startOfTheFirstPlaceholder];
            var firstInlines = memberTaggedText.ToInlines(classificationFormatMap, classificationTypeMap);
            allInlines.Add(new Run(prefixString));
            allInlines.AddRange(firstInlines);
            var suffixString = contentTemplate[(startOfTheFirstPlaceholder + "{0}".Length)..];
            allInlines.Add(new Run(suffixString));
            var toolTipTextBlock = allInlines.ToTextBlock(classificationFormatMap);
            toolTipTextBlock.FlowDirection = FlowDirection.LeftToRight;
            return toolTipTextBlock;
        }

        private static TextBlock Format(
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMap classificationFormatMap,
            string contentTemplate,
            ImmutableArray<TaggedText> firstTaggedText,
            ImmutableArray<TaggedText> secondTaggedText)
        {
            var allInlines = new List<Inline>();
            var startOfTheFirstPlaceholder = contentTemplate.IndexOf("{0}", StringComparison.Ordinal);

            var prefixString = contentTemplate[..startOfTheFirstPlaceholder];
            var firstInlines = firstTaggedText.ToInlines(classificationFormatMap, classificationTypeMap);
            allInlines.Add(new Run(prefixString));
            allInlines.AddRange(firstInlines);
            var startOfTheSecondPlaceholder = contentTemplate.IndexOf("{1}", StringComparison.Ordinal);
            var stringBetweenFirstAndSecondPlaceHolder = contentTemplate[(startOfTheFirstPlaceholder + "{0}".Length)..startOfTheSecondPlaceholder];
            allInlines.Add(new Run(stringBetweenFirstAndSecondPlaceHolder));
            var secondInlines = secondTaggedText.ToInlines(classificationFormatMap, classificationTypeMap);
            allInlines.AddRange(secondInlines);
            var suffixString = contentTemplate[(startOfTheSecondPlaceholder + "{1}".Length)..];
            allInlines.Add(new Run(suffixString));

            var toolTipTextBlock = allInlines.ToTextBlock(classificationFormatMap);
            toolTipTextBlock.FlowDirection = FlowDirection.LeftToRight;
            return toolTipTextBlock;
        }
    }
}
