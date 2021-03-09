// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.InheritanceChainMargin;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InheritanceChainMargin
{
    internal class InheritanceMarginTag : IGlyphTag
    {
        public readonly ImmutableDictionary<int, InheritanceChainMoniker> LineMargins;

        private InheritanceMarginTag(ImmutableDictionary<int, InheritanceChainMoniker> lineMargins)
        {
            LineMargins = lineMargins;
        }

        public static InheritanceMarginTag FromInheritanceInfo(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            Document document,
            ImmutableArray<LineInheritanceInfo> inheritanceInfo,
            CancellationToken cancellationToken)
        {
            var presentEntriesByLine = inheritanceInfo.GroupBy(
                    info => info.LineNumber,
                    info => info.InheritanceMembers)
                .ToImmutableDictionary(
                    keySelector: grouping => grouping.Key,
                    elementSelector: grouping => CreateInheritanceChainMoniker(
                        threadingContext,
                        streamingFindUsagesPresenter,
                        document,
                        grouping.SelectMany(g => g).ToImmutableArray(),
                        cancellationToken));
            return new InheritanceMarginTag(presentEntriesByLine);
        }

        private static InheritanceChainMoniker CreateInheritanceChainMoniker(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            Document document,
            ImmutableArray<InheritanceMemberItem> members,
            CancellationToken cancellationToken)
        {
            var aggregateRelationship = members
                .SelectMany(member => member.TargetItems.Select(target => target.RelationToMember))
                .Aggregate((r1, r2) => r1 | r2);
            var moniker = GetMonikers(aggregateRelationship);
            var memberPresentEntries = members
                .SelectAsArray(member => CreatePresentEntryForItem(threadingContext, streamingFindUsagesPresenter, document, member, cancellationToken));
            var tooltip = GetTooltip(members);
            return new InheritanceChainMoniker(moniker, tooltip, memberPresentEntries);
        }

        private static string GetTooltip(ImmutableArray<InheritanceMemberItem> members)
        {
            var tooltip = string.Empty;
            if (members.Length > 1)
            {
                tooltip = "View all members in the line";
            }
            else if (members.Length == 1)
            {
                var member = members[0];
                var targets = members[0].TargetItems;
                if (targets.Length == 1)
                {
                    var target = targets[0];
                    tooltip = targets[0].TargetDescription.Text + target.RelationToMember + members[0]  ;
                }
                else
                {
                    tooltip = member.MemberDescription.Tag + " " + member.MemberDescription.Text + " is ";
                }
            }

            return tooltip;
        }

        private static ImageMoniker GetMonikers(Relationship relationship)
            => relationship switch
            {
                Relationship.Overriding => KnownMonikers.Overriding,
                Relationship.Overriden => KnownMonikers.Overridden,
                Relationship.Overriding | Relationship.Overriden => KnownMonikers.Overriding,
                Relationship.Implementing => KnownMonikers.Implementing,
                Relationship.Implemented => KnownMonikers.Implemented,
                Relationship.Implementing | Relationship.Overriding => KnownMonikers.ImplementingOverridden,
                Relationship.Implementing | Relationship.Overriden => KnownMonikers.ImplementingOverriding,
                Relationship.Implemented | Relationship.Overriden => KnownMonikers.Implementing,
                _ => throw ExceptionUtilities.UnexpectedValue(relationship)
            };

        private static MemberPresentEntry CreatePresentEntryForItem(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            Document document,
            InheritanceMemberItem memberItem,
            CancellationToken cancellationToken)
        {
            var targets = memberItem.TargetItems;
            var navigableEntries = targets
                .SelectAsArray(item => CreatePresentEntryForTarget(threadingContext, streamingFindUsagesPresenter, document, item, cancellationToken));
            return new MemberPresentEntry("", memberItem.Glyph, navigableEntries);
        }

        private static TargetPresentEntry CreatePresentEntryForTarget(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            Document document,
            InheritanceTargetItem targetItem,
            CancellationToken cancellationToken)
            => new TargetPresentEntry(
                threadingContext,
                streamingFindUsagesPresenter,
                document,
                displayContent: targetItem.TargetDescription.Text,
                glyph: targetItem.Glyph,
                title: targetItem.TargetDescription.Text,
                definitionItems: targetItem.TargetDefinitionItems,
                cancellationToken: cancellationToken);
    }

    internal class InheritanceChainMoniker
    {
        public readonly ImageMoniker Moniker;
        public readonly string Tooltip;
        public readonly ImmutableArray<MemberPresentEntry> Members;

        public InheritanceChainMoniker(ImageMoniker moniker, string tooltip, ImmutableArray<MemberPresentEntry> members)
        {
            Moniker = moniker;
            Tooltip = tooltip;
            Members = members;
        }
    }

    internal class MemberPresentEntry
    {
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
        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private readonly Document _document;
        private readonly CancellationToken _cancellationToken;
        private readonly string _title;
        private readonly ImmutableArray<DefinitionItem> _definitionItems;

        public TargetPresentEntry(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            Document document,
            string displayContent,
            Glyph glyph,
            string title,
            ImmutableArray<DefinitionItem> definitionItems,
            CancellationToken cancellationToken)
        {
            _threadingContext = threadingContext;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _document = document;
            _cancellationToken = cancellationToken;
            _title = title;
            _definitionItems = definitionItems;
        }

        public Task NavigateToTargetAsync()
            => _streamingFindUsagesPresenter.TryNavigateToOrPresentItemsAsync(
                _threadingContext,
                _document.Project.Solution.Workspace,
                _title,
                _definitionItems,
                _cancellationToken);
    }
}
