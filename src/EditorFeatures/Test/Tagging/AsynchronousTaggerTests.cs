// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Tagging;

[UseExportProvider]
public sealed class AsynchronousTaggerTests
{
    /// <summary>
    /// This hits a special codepath in the product that is optimized for more than 100 spans.
    /// I'm leaving this test here because it covers that code path (as shown by code coverage)
    /// </summary>
    [WpfTheory]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530368")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1927519")]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(100, 50)]
    [InlineData(101, 50)]
    public async Task LargeNumberOfSpans(int tagsProduced, int expectedCount)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("""
            class Program
            {
                void M()
                {
                    int z = 0;
                    z = z + z + z + z + z + z + z + z + z + z +
                        z + z + z + z + z + z + z + z + z + z +
                        z + z + z + z + z + z + z + z + z + z +
                        z + z + z + z + z + z + z + z + z + z +
                        z + z + z + z + z + z + z + z + z + z +
                        z + z + z + z + z + z + z + z + z + z +
                        z + z + z + z + z + z + z + z + z + z +
                        z + z + z + z + z + z + z + z + z + z +
                        z + z + z + z + z + z + z + z + z + z +
                        z + z + z + z + z + z + z + z + z + z;
                }
            }
            """);

        var asyncListener = new AsynchronousOperationListener();

        WpfTestRunner.RequireWpfFact($"{nameof(AsynchronousTaggerTests)}.{nameof(LargeNumberOfSpans)} creates asynchronous taggers");

        var eventSource = CreateEventSource();
        var taggerProvider = new TestTaggerProvider(
            workspace.GetService<IThreadingContext>(),
            (s, c) => Enumerable
                .Range(0, tagsProduced)
                .Select(i => new TagSpan<TextMarkerTag>(new SnapshotSpan(s.Snapshot, new Span(50 + i * 2, 1)), new TextMarkerTag($"Test{i}"))),
            eventSource,
            workspace.GetService<IGlobalOptionService>(),
            asyncListener);

        var document = workspace.Documents.First();
        var textBuffer = document.GetTextBuffer();
        var snapshot = textBuffer.CurrentSnapshot;
        using var tagger = taggerProvider.CreateTagger(textBuffer);
        Contract.ThrowIfNull(tagger);

        var snapshotSpans = new NormalizedSnapshotSpanCollection(
            snapshot, Enumerable.Range(0, 101).Select(i => new Span(i * 4, 1)));

        eventSource.SendUpdateEvent();

        await asyncListener.ExpeditedWaitAsync();

        var tags = tagger.GetTags(snapshotSpans);
        Assert.Equal(expectedCount, tags.Count());
    }

    [WpfFact]
    public void TestNotSynchronousOutlining()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("""
            class Program {

            }
            """, composition: EditorTestCompositions.EditorFeaturesWpf);
        WpfTestRunner.RequireWpfFact($"{nameof(AsynchronousTaggerTests)}.{nameof(TestNotSynchronousOutlining)} creates asynchronous taggers");

        var tagProvider = workspace.ExportProvider.GetExportedValue<AbstractStructureTaggerProvider>();

        var document = workspace.Documents.First();
        var textBuffer = document.GetTextBuffer();
        using var tagger = tagProvider.CreateTagger(textBuffer);
        Contract.ThrowIfNull(tagger);

        // The very first all to get tags will not be synchronous as this contains no #region tag
        var tags = tagger.GetTags(new NormalizedSnapshotSpanCollection(textBuffer.CurrentSnapshot.GetFullSpan()));
        Assert.Equal(0, tags.Count());
    }

    [WpfFact]
    public void TestSynchronousOutlining()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("""
            #region x

            class Program
            {
            }

            #endregion
            """, composition: EditorTestCompositions.EditorFeaturesWpf);
        WpfTestRunner.RequireWpfFact($"{nameof(AsynchronousTaggerTests)}.{nameof(TestSynchronousOutlining)} creates asynchronous taggers");

        var tagProvider = workspace.ExportProvider.GetExportedValue<AbstractStructureTaggerProvider>();

        var document = workspace.Documents.First();
        var textBuffer = document.GetTextBuffer();
        using var tagger = tagProvider.CreateTagger(textBuffer);
        Contract.ThrowIfNull(tagger);

        // The very first all to get tags will be synchronous because of the #region
        var tags = tagger.GetTags(new NormalizedSnapshotSpanCollection(textBuffer.CurrentSnapshot.GetFullSpan()));
        Assert.Equal(2, tags.Count());
    }

    private static TestTaggerEventSource CreateEventSource()
        => new();

    private sealed class TestTaggerProvider(
        IThreadingContext threadingContext,
        Func<SnapshotSpan, CancellationToken, IEnumerable<ITagSpan<TextMarkerTag>>> callback,
        ITaggerEventSource eventSource,
        IGlobalOptionService globalOptions,
        IAsynchronousOperationListener asyncListener)
        : AsynchronousTaggerProvider<TextMarkerTag>(threadingContext, globalOptions, visibilityTracker: null, asyncListener)
    {
        protected override TaggerDelay EventChangeDelay
            => TaggerDelay.NearImmediate;

        protected override ITaggerEventSource CreateEventSource(ITextView? textView, ITextBuffer subjectBuffer)
            => eventSource;

        protected override Task ProduceTagsAsync(
            TaggerContext<TextMarkerTag> context, DocumentSnapshotSpan snapshotSpan, int? caretPosition, CancellationToken cancellationToken)
        {
            foreach (var tag in callback(snapshotSpan.SnapshotSpan, cancellationToken))
                context.AddTag(tag);

            return Task.CompletedTask;
        }

        protected override bool TagEquals(TextMarkerTag tag1, TextMarkerTag tag2)
            => tag1 == tag2;
    }

    private sealed class TestTaggerEventSource() : AbstractTaggerEventSource
    {
        public void SendUpdateEvent()
            => this.RaiseChanged();

        public override void Connect()
        {
        }

        public override void Disconnect()
        {
        }
    }
}
