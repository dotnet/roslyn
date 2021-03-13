// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin
{
    internal class InheritanceMarginTag : IGlyphTag
    {
        public readonly ImageMoniker Moniker;
        public readonly string Tooltip;
        public readonly ImmutableArray<MemberPresentEntry> Members;

        public InheritanceMarginTag(ImageMoniker moniker, string tooltip, ImmutableArray<MemberPresentEntry> members)
        {
            Moniker = moniker;
            Tooltip = tooltip;
            Members = members;
        }

        public static InheritanceMarginTag FromInheritanceInfo(ImmutableArray<InheritanceMemberItem> membersOnTheLine)
        {
            Debug.Assert(!membersOnTheLine.IsEmpty);

            var aggregateRelationship = membersOnTheLine
                .SelectMany(member => member.TargetItems.Select(target => target.RelationToMember))
                .Aggregate((r1, r2) => r1 | r2);
            var moniker = GetMonikers(aggregateRelationship);
            var tooltip = GetTooltip(membersOnTheLine);
            var presentEntriesByLine = membersOnTheLine
                .SelectAsArray(CreatePresentEntryForItem);
            return new InheritanceMarginTag(moniker, tooltip, presentEntriesByLine);
        }

        private static string GetTooltip(ImmutableArray<InheritanceMemberItem> members)
        {
            // TODO: All the strings should be put into resources
            string tooltip;
            if (members.Length > 1)
            {
                tooltip = "Click to view all members in the line";
            }
            else if (members.Length == 1)
            {
                var member = members[0];
                var memberName = members[0].MemberDescription.Text;
                var targets = members[0].TargetItems;
                if (targets.Length == 1)
                {
                    var target = targets[0];
                    var relationship = target.RelationToMember;
                    var targetName = target.TargetDescription.Text;
                    tooltip = relationship switch
                    {
                        InheritanceRelationship.Implemented => $"{memberName} implements {targetName}, click to navigate",
                        InheritanceRelationship.Implementing => $"{targetName} implements {memberName}, click to navigate",
                        InheritanceRelationship.Overriden => $"{targetName} overrides {memberName}, click to navigate",
                        InheritanceRelationship.Overriding => $"{memberName} overrides {targetName}, click to navigate",
                        _ => throw ExceptionUtilities.Unreachable,
                    };
                }
                else
                {
                    tooltip = $"Click to view all inherited and base members";
                }
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(members.Length);
            }

            return tooltip;
        }

        private static ImageMoniker GetMonikers(InheritanceRelationship inheritanceRelationship)
            => inheritanceRelationship switch
            {
                InheritanceRelationship.Implementing | InheritanceRelationship.Overriding => KnownMonikers.ImplementingOverriding,
                InheritanceRelationship.Implementing | InheritanceRelationship.Overriden => KnownMonikers.ImplementingOverridden,
                _ when inheritanceRelationship.HasFlag(InheritanceRelationship.Implemented) => KnownMonikers.Implemented,
                _ when inheritanceRelationship.HasFlag(InheritanceRelationship.Implementing) => KnownMonikers.Implementing,
                _ when inheritanceRelationship.HasFlag(InheritanceRelationship.Overriden) => KnownMonikers.Overridden,
                _ when inheritanceRelationship.HasFlag(InheritanceRelationship.Overriding) => KnownMonikers.Overriding,
                _ => throw ExceptionUtilities.Unreachable,
            };

        private static MemberPresentEntry CreatePresentEntryForItem(
            InheritanceMemberItem memberItem)
        {
            var targets = memberItem.TargetItems;
            var navigableEntries = targets
                .SelectAsArray(CreatePresentEntryForTarget);
            return new MemberPresentEntry(memberItem.MemberDescription.Text, memberItem.Glyph, navigableEntries);
        }

        private static TargetPresentEntry CreatePresentEntryForTarget(
            InheritanceTargetItem targetItem)
            => new TargetPresentEntry(
                glyph: targetItem.Glyph,
                title: targetItem.TargetDescription.Text,
                definitionItems: targetItem.TargetDefinitionItems);
    }

    internal class MemberPresentEntry
    {
        public readonly string Name;
        public readonly string DisplayContent;
        public readonly Glyph Glyph;
        public readonly ImmutableArray<TargetPresentEntry> Targets;

        public MemberPresentEntry(
            string displayContent,
            Glyph glyph,
            ImmutableArray<TargetPresentEntry> targets)
        {
            DisplayContent = displayContent;
            Glyph = glyph;
            Targets = targets;
        }
    }

    internal class TargetPresentEntry
    {
        public readonly string Title;
        public readonly ImmutableArray<DefinitionItem> DefinitionItems;
        public readonly string Name;

        public TargetPresentEntry(
            Glyph glyph,
            string title,
            ImmutableArray<DefinitionItem> definitionItems)
        {
        }
    }
}
