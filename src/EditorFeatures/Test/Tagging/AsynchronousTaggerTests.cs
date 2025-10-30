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
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        WpfTestRunner.RequireWpfFact($"{nameof(AsynchronousTaggerTests)}.{nameof(LargeNumberOfSpans)} creates asynchronous taggers");

        var asyncListenerProvider = workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        var asyncListener = (AsynchronousOperationListener)asyncListenerProvider.GetListener(FeatureAttribute.Tagger);

        var eventSource = new TestTaggerEventSource();
        var taggerProvider = new TestTaggerProvider(
            workspace.GetService<TaggerHost>(),
            (_, s) => Enumerable
                .Range(0, tagsProduced)
                .Select(i => new TagSpan<TextMarkerTag>(new SnapshotSpan(s.SnapshotSpan.Snapshot, new Span(50 + i * 2, 1)), new TextMarkerTag($"Test{i}"))),
            eventSource,
            supportsFrozenPartialSemantics: false,
            FeatureAttribute.Tagger);

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
            """, composition: EditorTestCompositions.EditorFeatures);
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
            """, composition: EditorTestCompositions.EditorFeatures);
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

    [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2016199")]
    public async Task TestFrozenPartialSemantics1()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("""
            class Program
            {
            }
            """);

        WpfTestRunner.RequireWpfFact($"{nameof(AsynchronousTaggerTests)}.{nameof(TestFrozenPartialSemantics1)} creates asynchronous taggers");

        var testDocument = workspace.Documents.First();

        var asyncListenerProvider = workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        var asyncListener = (AsynchronousOperationListener)asyncListenerProvider.GetListener(FeatureAttribute.Tagger);

        var eventSource = new TestTaggerEventSource();
        var callbackCounter = 0;
        var taggerProvider = new TestTaggerProvider(
            workspace.GetService<TaggerHost>(),
            (c, s) =>
            {
                Assert.True(callbackCounter <= 1);
                var document = workspace.CurrentSolution.GetRequiredDocument(testDocument.Id);
                if (callbackCounter is 0)
                {
                    Assert.True(c.FrozenPartialSemantics);
                    // Should be getting a frozen document here.
                    Assert.NotSame(document, c.SpansToTag.First().Document);
                }
                else
                {
                    Assert.False(c.FrozenPartialSemantics);
                    Assert.Same(document, c.SpansToTag.First().Document);
                }

                callbackCounter++;
                return [new TagSpan<TextMarkerTag>(new SnapshotSpan(s.SnapshotSpan.Snapshot, new Span(0, 1)), new TextMarkerTag($"Test"))];
            },
            eventSource,
            supportsFrozenPartialSemantics: true,
            FeatureAttribute.Tagger);

        var textBuffer = testDocument.GetTextBuffer();
        using var tagger = taggerProvider.CreateTagger(textBuffer);
        Contract.ThrowIfNull(tagger);

        eventSource.SendUpdateEvent();

        await asyncListener.ExpeditedWaitAsync();
        Assert.Equal(2, callbackCounter);

        var tags = tagger.GetTags(new NormalizedSnapshotSpanCollection(textBuffer.CurrentSnapshot.GetFullSpan()));
        Assert.Equal(1, tags.Count());
    }

    [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2016199")]
    public async Task TestFrozenPartialSemantics2()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("""
            class Program
            {
            }
            """);

        WpfTestRunner.RequireWpfFact($"{nameof(AsynchronousTaggerTests)}.{nameof(TestFrozenPartialSemantics2)} creates asynchronous taggers");

        var testDocument = workspace.Documents.First();

        var asyncListenerProvider = workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        var asyncListener = (AsynchronousOperationListener)asyncListenerProvider.GetListener(FeatureAttribute.Tagger);

        var eventSource = new TestTaggerEventSource();
        var callbackCounter = 0;
        var taggerProvider = new TestTaggerProvider(
            workspace.GetService<TaggerHost>(),
            (c, s) =>
            {
                var document = workspace.CurrentSolution.GetRequiredDocument(testDocument.Id);
                Assert.True(callbackCounter == 0);
                Assert.False(c.FrozenPartialSemantics);
                Assert.Same(document, c.SpansToTag.First().Document);

                callbackCounter++;
                return [new TagSpan<TextMarkerTag>(new SnapshotSpan(s.SnapshotSpan.Snapshot, new Span(0, 1)), new TextMarkerTag($"Test"))];
            },
            eventSource,
            supportsFrozenPartialSemantics: false,
            FeatureAttribute.Tagger);

        var textBuffer = testDocument.GetTextBuffer();
        using var tagger = taggerProvider.CreateTagger(textBuffer);
        Contract.ThrowIfNull(tagger);

        eventSource.SendUpdateEvent();

        await asyncListener.ExpeditedWaitAsync();
        Assert.Equal(1, callbackCounter);

        var tags = tagger.GetTags(new NormalizedSnapshotSpanCollection(textBuffer.CurrentSnapshot.GetFullSpan()));
        Assert.Equal(1, tags.Count());
    }

    private sealed class TestTaggerProvider(
        TaggerHost taggerHost,
        Func<TaggerContext<TextMarkerTag>, DocumentSnapshotSpan, IEnumerable<TagSpan<TextMarkerTag>>> callback,
        ITaggerEventSource eventSource,
        bool supportsFrozenPartialSemantics,
        string featureName)
        : AsynchronousTaggerProvider<TextMarkerTag>(taggerHost, featureName)
    {
        protected override TaggerDelay EventChangeDelay
            => TaggerDelay.NearImmediate;

        protected override bool SupportsFrozenPartialSemantics => supportsFrozenPartialSemantics;

        protected override ITaggerEventSource CreateEventSource(ITextView? textView, ITextBuffer subjectBuffer)
            => eventSource;

        protected override Task ProduceTagsAsync(
            TaggerContext<TextMarkerTag> context, DocumentSnapshotSpan snapshotSpan, int? caretPosition, CancellationToken cancellationToken)
        {
            foreach (var tag in callback(context, snapshotSpan))
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
