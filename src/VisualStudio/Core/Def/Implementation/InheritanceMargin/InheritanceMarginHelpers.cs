// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;
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
            = ImmutableArray.Create(
                InheritanceRelationship.DerivedType,
                InheritanceRelationship.OverridingMember);

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

            if (s_relationships_Shown_As_I_Up_Arrow.Any(flag => inheritanceRelationship.HasFlag(flag))
                && s_relationships_Shown_As_I_Down_Arrow.Any(flag => inheritanceRelationship.HasFlag(flag)))
            {
                return KnownMonikers.ImplementingImplemented;
            }

            if (s_relationships_Shown_As_O_Up_Arrow.Any(flag => inheritanceRelationship.HasFlag(flag))
                && s_relationships_Shown_As_O_Down_Arrow.Any(flag => inheritanceRelationship.HasFlag(flag)))
            {
                return KnownMonikers.OverridingOverridden;
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

        public static ImmutableArray<MenuItemViewModel> CreateMenuItemViewModelsForSingleMember(ImmutableArray<InheritanceTargetItem> targets)
            => targets.OrderBy(target => target.DisplayName)
                .GroupBy(target => target.RelationToMember)
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
            // For multiple members, check if all the targets have the same inheritance relationship.
            // If so, then don't add the header, because it is already indicated by the margin.
            // Otherwise, add the Header.
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
    }
}
