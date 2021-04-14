// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
{
    internal static class InheritanceMarginHelpers
    {
        /// <summary>
        /// Decide which moniker should be shown.
        /// </summary>
        public static ImageMoniker GetMoniker(InheritanceRelationship inheritanceRelationship)
        {
            //  If there are multiple targets and we have the corresponding compound image, use it
            if (inheritanceRelationship.HasFlag(InheritanceRelationship.ImplementingOverriding))
            {
                return KnownMonikers.ImplementingOverriding;
            }

            if (inheritanceRelationship.HasFlag(InheritanceRelationship.ImplementingOverridden))
            {
                return KnownMonikers.ImplementingOverridden;
            }

            // Otherwise, show the image based on this preference
            if (inheritanceRelationship.HasFlag(InheritanceRelationship.Implemented))
            {
                return KnownMonikers.Implemented;
            }

            if (inheritanceRelationship.HasFlag(InheritanceRelationship.Implementing))
            {
                return KnownMonikers.Implementing;
            }

            if (inheritanceRelationship.HasFlag(InheritanceRelationship.Overridden))
            {
                return KnownMonikers.Overridden;
            }

            if (inheritanceRelationship.HasFlag(InheritanceRelationship.Overriding))
            {
                return KnownMonikers.Overriding;
            }

            // The relationship is None. Don't know what image should be shown, throws
            throw ExceptionUtilities.UnexpectedValue(inheritanceRelationship);
        }

        public static ImmutableArray<MenuItemViewModel> CreateMenuItemViewModelsForSingleMember(ImmutableArray<InheritanceTargetItem> targets)
        {
            var targetsByRelationship = targets.GroupBy(target => target.RelationToMember).ToImmutableArray();
            if (targetsByRelationship.Length == 1)
            {
                // If all targets have one relationship.
                // e.g. interface IBar { void Bar(); }
                // class A : IBar { void Bar() {} }
                // class B : IBar { void Bar() {} }
                // for 'IBar', the margin would be I↓. So separator is not needed.
                return targets.SelectAsArray(TargetMenuItemViewModel.Create).CastArray<MenuItemViewModel>();
            }
            else
            {
                // Otherwise, it means these targets has different relationship,
                // the targets would be shown in group, and a separator should be shown as the first item to indicate the relationship to user.
                return targetsByRelationship
                    .SelectMany((grouping, index) => CreateMenuItemsWithSeparator(grouping.SelectAsArray(g => g), grouping.Key, index == 0))
                    .ToImmutableArray();
            }
        }

        public static ImmutableArray<MenuItemViewModel> CreateMenuItemViewModelsForMultipleMembers(ImmutableArray<InheritanceMarginItem> members)
        {
            Contract.ThrowIfTrue(members.Length <= 1);
            // For multiple members, check if all the targets have the same inheritance relationship.
            // If so, then don't add the separator, because it is already indicated by the margin.
            // Otherwise, show the separator.
            var set = members
                .SelectMany(member => member.TargetItems.Select(item => item.RelationToMember))
                .ToImmutableHashSet();
            if (set.Count == 1)
            {
                return members.SelectAsArray(MemberMenuItemViewModel.CreateWithNoSeparator).CastArray<MenuItemViewModel>();
            }
            else
            {
                return members.SelectAsArray(MemberMenuItemViewModel.CreateWithSeparator).CastArray<MenuItemViewModel>();
            }
        }

        public static ImmutableArray<MenuItemViewModel> CreateMenuItemsWithSeparator(
            ImmutableArray<InheritanceTargetItem> targets,
            InheritanceRelationship relationship,
            bool isTheSeparatorTheFirstItem)
        {
            using var _ = CodeAnalysis.PooledObjects.ArrayBuilder<MenuItemViewModel>.GetInstance(out var builder);
            var separatorViewModel = new SeparatorViewModel(isTheSeparatorTheFirstItem, GetMoniker(relationship));
            builder.Add(separatorViewModel);
            foreach (var targetItem in targets)
            {
                builder.Add(TargetMenuItemViewModel.Create(targetItem));
            }

            return builder.ToImmutable();
        }
    }
}
