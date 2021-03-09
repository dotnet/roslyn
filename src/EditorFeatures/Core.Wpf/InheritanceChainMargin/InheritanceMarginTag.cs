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
                        grouping.SelectMany(g => g).ToImmutableArray(), cancellationToken));
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

            if (aggregateRelationship == Relationship.Implemented)
            {

            }
            else if (aggregateRelationship == Relationship.Implementing)
            {

            }
            else if ()
            KnownMonikers.Implemented;
            KnownMonikers.Implementing;
            KnownMonikers.Overridden;
            KnownMonikers.Overriding;
            KnownMonikers.ImplementingOverriden;
            KnownMonikers.ImplementingOverriding;



            return new InheritanceChainMoniker();
        }

        private static ImageMoniker GetMonikers(Relationship relationship)
            => relationship switch
            {
                Relationship.Implementing => KnownMonikers.Implementing,
                Relationship.Implemented => KnownMonikers.Implemented,
                Relationship.Overriding => KnownMonikers.Overriding,
                Relationship.Overriden => KnownMonikers.Overridden,
                Relationship.Implementing | Relationship.Overriding => KnownMonikers.ImplementingOverriden,
                Relationship.Implementing | Relationship.Overriden => KnownMonikers.ImplementingOverriding,
                _ => Relationship.Implementing,
            }

        private static MemberPresentEntry CreatePresentEntryForItem(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            InheritanceMemberItem memberItem,
            CancellationToken cancellationToken)
        {
            var targets = memberItem.TargetItems;
            var navigableEntries = targets.SelectAsArray(item => new NavigableEntry(item));
            return new MemberPresentEntry("", navigableEntries);
        }

        private static TargetPresentEntry CreatePresentEntryForTarget(
            InheritanceTargetItem targetItem,
            CancellationToken cancellationToken)
        {
            var documentSpans = targetItem.TargetDefinitionItems;
        }
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
        private readonly ImmutableArray<DefinitionItem> _items;

        public TargetPresentEntry(
            string displayContent,
            Glyph glyph,
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            Document document,
            CancellationToken cancellationToken,
            string title,
            ImmutableArray<DefinitionItem> items)
        {
            _threadingContext = threadingContext;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _document = document;
            _cancellationToken = cancellationToken;
            _title = title;
            _items = items;
        }

        public Task NavigateToTargetAsync()
            => _streamingFindUsagesPresenter.TryNavigateToOrPresentItemsAsync(
                _threadingContext,
                _document.Project.Solution.Workspace,
                _title,
                _items,
                _cancellationToken);
    }
}
