// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging.TagSources
{
    internal sealed class SemanticBufferTagSource<TTag> : ProducerPopulatedTagSource<TTag> where TTag : ITag
    {
        private VersionStamp _lastSemanticVersion;

        public SemanticBufferTagSource(
            ITextBuffer subjectBuffer,
            IAsynchronousTaggerDataSource<TTag> dataSource,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
                : base(/*textViewOpt:*/ null, subjectBuffer, dataSource, asyncListener, notificationService)
        {
            _lastSemanticVersion = VersionStamp.Default;
        }

        protected override IList<SnapshotSpan> GetInitialSpansToTag()
        {
            return new[] { SubjectBuffer.CurrentSnapshot.GetFullSpan() };
        }

        protected override SnapshotPoint? GetCaretPoint()
        {
            return null;
        }

        protected override async Task RecomputeTagsAsync(
            SnapshotPoint? caret, TextChangeRange? range, IEnumerable<DocumentSnapshotSpan> spansToCompute, CancellationToken cancellationToken)
        {
            this.WorkQueue.AssertIsBackground();

            // we should have only one
            var tuple = spansToCompute.Single();

            // split data
            var span = tuple.SnapshotSpan;
            var document = tuple.Document;
            if (document == null)
            {
                // given span is not part of our workspace, let base tag source handle this case.
                await base.RecomputeTagsAsync(caret, range, spansToCompute, cancellationToken).ConfigureAwait(false);
                return;
            }

            var newVersion = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            await RecomputeTagsAsync(range, spansToCompute, document, span.Snapshot, _lastSemanticVersion, newVersion, cancellationToken).ConfigureAwait(false);

            // this is only place where the version is updated
            _lastSemanticVersion = newVersion;
        }

        private async Task RecomputeTagsAsync(
            TextChangeRange? range, IEnumerable<DocumentSnapshotSpan> spansToCompute,
            Document document, ITextSnapshot snapshot,
            VersionStamp oldVersion, VersionStamp newVersion, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // there is no accumulate data, check whether we already reported this
            if (range == null)
            {
                // active file can be called twice for the same top level edit (the very last top level edit). 
                // one from text edit event source and one from semantic change event source.
                // this make sure that when we are called to recompute due to semantic change event source, 
                // we haven't already recompute it by text edits event source.
                // for opened files that are not active, it should be called by semantic change event source and recompute tags for whole file.
                if (newVersion != oldVersion)
                {
                    // we didn't report this yet
                    await base.RecomputeTagsAsync(null, range, spansToCompute, cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            // there was top level edit, check whether that edit updated top level element
            var service = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            if (service == null || newVersion != oldVersion)
            {
                // we have newer version, refresh whole buffer
                await base.RecomputeTagsAsync(null, range, spansToCompute, cancellationToken).ConfigureAwait(false);
                return;
            }

            // no top level edits, find out member that has changed
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var changedSpan = new TextSpan(range.Value.Span.Start, range.Value.NewLength);
            var member = service.GetContainingMemberDeclaration(root, range.Value.Span.Start);
            if (member == null || !member.FullSpan.Contains(changedSpan))
            {
                // no top level edit, but out side a member has changed. for now, just re-colorize whole file
                await base.RecomputeTagsAsync(null, range, spansToCompute, cancellationToken).ConfigureAwait(false);
                return;
            }

            // perf optimization.
            // semantic classifier has two levels of perf optimization.
            // first, it will check whether all edits since the last update has happened locally. if it did, it will find the member
            //        that contains the changes and only refresh that member
            // second, when it can do the first, it will check whether it can even get more perf improvement by using speculative semantic model.
            //         if it can, it will re-adjust span so that it can use speculative binding.
            var refreshSpan = service.GetMemberBodySpanForSpeculativeBinding(member);
            var rangeToRecompute = new SnapshotSpan(snapshot, refreshSpan.Contains(changedSpan) ? refreshSpan.ToSpan() : member.FullSpan.ToSpan());

            // re-colorize only a member
            await base.RecomputeTagsAsync(
                null, range, SpecializedCollections.SingletonEnumerable(new DocumentSnapshotSpan(document, rangeToRecompute)), cancellationToken).ConfigureAwait(false);
        }
    }
}
