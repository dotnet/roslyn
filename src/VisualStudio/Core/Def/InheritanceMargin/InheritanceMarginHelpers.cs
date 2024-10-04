// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin;

internal static class InheritanceMarginHelpers
{
    private static readonly ObjectPool<MultiDictionary<string, InheritanceTargetItem>> s_pool = new(() => new());

    private static readonly ImmutableArray<InheritanceRelationship> s_relationships_Shown_As_I_Up_Arrow
        =
        [
            InheritanceRelationship.ImplementedInterface,
            InheritanceRelationship.InheritedInterface,
            InheritanceRelationship.ImplementedMember,
        ];

    private static readonly ImmutableArray<InheritanceRelationship> s_relationships_Shown_As_I_Down_Arrow
        = [InheritanceRelationship.ImplementingType, InheritanceRelationship.ImplementingMember];

    private static readonly ImmutableArray<InheritanceRelationship> s_relationships_Shown_As_O_Up_Arrow
        = [InheritanceRelationship.BaseType, InheritanceRelationship.OverriddenMember];

    private static readonly ImmutableArray<InheritanceRelationship> s_relationships_Shown_As_O_Down_Arrow
        = [InheritanceRelationship.DerivedType, InheritanceRelationship.OverridingMember];

    /// <summary>
    /// Decide which moniker should be shown.
    /// </summary>
    public static ImageMoniker GetMoniker(InheritanceRelationship inheritanceRelationship)
    {
        //  If there are multiple targets and we have the corresponding compound image, use it
        if (s_relationships_Shown_As_I_Up_Arrow.Any(static (flag, inheritanceRelationship) => inheritanceRelationship.HasFlag(flag), inheritanceRelationship)
            && s_relationships_Shown_As_O_Down_Arrow.Any(static (flag, inheritanceRelationship) => inheritanceRelationship.HasFlag(flag), inheritanceRelationship))
        {
            return KnownMonikers.ImplementingOverridden;
        }

        if (s_relationships_Shown_As_I_Up_Arrow.Any(static (flag, inheritanceRelationship) => inheritanceRelationship.HasFlag(flag), inheritanceRelationship)
            && s_relationships_Shown_As_O_Up_Arrow.Any(static (flag, inheritanceRelationship) => inheritanceRelationship.HasFlag(flag), inheritanceRelationship))
        {
            return KnownMonikers.ImplementingOverriding;
        }

        if (s_relationships_Shown_As_I_Up_Arrow.Any(static (flag, inheritanceRelationship) => inheritanceRelationship.HasFlag(flag), inheritanceRelationship)
            && s_relationships_Shown_As_I_Down_Arrow.Any(static (flag, inheritanceRelationship) => inheritanceRelationship.HasFlag(flag), inheritanceRelationship))
        {
            return KnownMonikers.ImplementingImplemented;
        }

        if (s_relationships_Shown_As_O_Up_Arrow.Any(static (flag, inheritanceRelationship) => inheritanceRelationship.HasFlag(flag), inheritanceRelationship)
            && s_relationships_Shown_As_O_Down_Arrow.Any(static (flag, inheritanceRelationship) => inheritanceRelationship.HasFlag(flag), inheritanceRelationship))
        {
            return KnownMonikers.OverridingOverridden;
        }

        // Otherwise, show the image based on this preference
        if (s_relationships_Shown_As_I_Up_Arrow.Any(static (flag, inheritanceRelationship) => inheritanceRelationship.HasFlag(flag), inheritanceRelationship))
        {
            return KnownMonikers.Implementing;
        }

        if (s_relationships_Shown_As_I_Down_Arrow.Any(static (flag, inheritanceRelationship) => inheritanceRelationship.HasFlag(flag), inheritanceRelationship))
        {
            return KnownMonikers.Implemented;
        }

        if (s_relationships_Shown_As_O_Up_Arrow.Any(static (flag, inheritanceRelationship) => inheritanceRelationship.HasFlag(flag), inheritanceRelationship))
        {
            return KnownMonikers.Overriding;
        }

        if (s_relationships_Shown_As_O_Down_Arrow.Any(static (flag, inheritanceRelationship) => inheritanceRelationship.HasFlag(flag), inheritanceRelationship))
        {
            return KnownMonikers.Overridden;
        }

        if (inheritanceRelationship.HasFlag(InheritanceRelationship.InheritedImport))
            return KnownMonikers.NamespaceShortcut;

        // The relationship is None. Don't know what image should be shown, throws
        throw ExceptionUtilities.UnexpectedValue(inheritanceRelationship);
    }

    public static ImmutableArray<MenuItemViewModel> CreateModelsForMarginItem(InheritanceMarginItem item)
    {
        var nameToTargets = s_pool.Allocate();
        try
        {
            // Create a mapping from display name to all targets with that name.  This will allow us to determine if
            // there may be multiple results with the same name, so we can disambiguate them with additional
            // information later on when we create the items.
            var targets = item.TargetItems;
            foreach (var target in targets)
                nameToTargets.Add(target.DisplayName, target);

            return item.TargetItems
                .GroupBy(t => t.RelationToMember)
                .SelectMany(g => CreateMenuItemsWithHeader(item, g.Key, g, nameToTargets))
                .ToImmutableArray();
        }
        finally
        {
            nameToTargets.Clear();
            s_pool.Free(nameToTargets);
        }
    }

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
        return members.SelectAsArray(m => new MemberMenuItemViewModel(
            m.DisplayTexts.JoinText(),
            m.Glyph.GetImageMoniker(),
            CreateModelsForMarginItem(m))).CastArray<MenuItemViewModel>();
    }

    public static ImmutableArray<MenuItemViewModel> CreateMenuItemsWithHeader(
        InheritanceMarginItem item,
        InheritanceRelationship relationship,
        IEnumerable<InheritanceTargetItem> targets,
        MultiDictionary<string, InheritanceTargetItem> nameToTargets)
    {
        using var _ = ArrayBuilder<MenuItemViewModel>.GetInstance(out var builder);
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
            InheritanceRelationship.InheritedImport => item.DisplayTexts.JoinText(),
            _ => throw ExceptionUtilities.UnexpectedValue(relationship)
        };

        builder.Add(new HeaderMenuItemViewModel(displayContent, GetMoniker(relationship)));
        foreach (var target in targets)
        {
            var targetsWithSameName = nameToTargets[target.DisplayName];
            if (targetsWithSameName.Count >= 2)
            {
                // Two or more items with the same name.  Try to disambiguate them based on their languages if
                // they're all distinct, or their project name if they're not.
                var distinctLanguageCount = targetsWithSameName.Select(t => t.LanguageGlyph).Distinct().Count();
                if (distinctLanguageCount == targetsWithSameName.Count)
                {
                    builder.Add(DisambiguousTargetMenuItemViewModel.CreateWithSourceLanguageGlyph(target));
                    continue;
                }

                if (target.ProjectName != null)
                {
                    builder.Add(TargetMenuItemViewModel.Create(
                        target, string.Format(ServicesVSResources._0_1, target.DisplayName, target.ProjectName)));
                    continue;
                }
            }

            builder.Add(TargetMenuItemViewModel.Create(target, target.DisplayName));
        }

        return builder.ToImmutableAndClear();
    }
}
