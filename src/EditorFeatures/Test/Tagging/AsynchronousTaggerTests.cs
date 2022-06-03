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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Tagging
{
    [UseExportProvider]
    public class AsynchronousTaggerTests : TestBase
    {
        /// <summary>
        /// This hits a special codepath in the product that is optimized for more than 100 spans.
        /// I'm leaving this test here because it covers that code path (as shown by code coverage)
        /// </summary>
        [WpfFact]
        [WorkItem(530368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530368")]
        public async Task LargeNumberOfSpans()
        {
            using var workspace = TestWorkspace.CreateCSharp(@"class Program
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
}");
            static List<ITagSpan<TestTag>> tagProducer(SnapshotSpan span, CancellationToken cancellationToken)
            {
                return new List<ITagSpan<TestTag>>() { new TagSpan<TestTag>(span, new TestTag()) };
            }

            var asyncListener = new AsynchronousOperationListener();

            WpfTestRunner.RequireWpfFact($"{nameof(AsynchronousTaggerTests)}.{nameof(LargeNumberOfSpans)} creates asynchronous taggers");

            var eventSource = CreateEventSource();
            var taggerProvider = new TestTaggerProvider(
                workspace.GetService<IThreadingContext>(),
                tagProducer,
                eventSource,
                workspace.GetService<IGlobalOptionService>(),
                asyncListener);

            var document = workspace.Documents.First();
            var textBuffer = document.GetTextBuffer();
            var snapshot = textBuffer.CurrentSnapshot;
            var tagger = taggerProvider.CreateTagger<TestTag>(textBuffer);
            Contract.ThrowIfNull(tagger);

            using var disposable = (IDisposable)tagger;
            var spans = Enumerable.Range(0, 101).Select(i => new Span(i * 4, 1));
            var snapshotSpans = new NormalizedSnapshotSpanCollection(snapshot, spans);

            eventSource.SendUpdateEvent();

            await asyncListener.ExpeditedWaitAsync();

            var tags = tagger.GetTags(snapshotSpans);

            Assert.Equal(1, tags.Count());
        }

        [WpfFact]
        public void TestNotSynchronousOutlining()
        {
            using var workspace = TestWorkspace.CreateCSharp("class Program {\r\n\r\n}", composition: EditorTestCompositions.EditorFeaturesWpf);
            WpfTestRunner.RequireWpfFact($"{nameof(AsynchronousTaggerTests)}.{nameof(TestNotSynchronousOutlining)} creates asynchronous taggers");

            var tagProvider = workspace.ExportProvider.GetExportedValue<AbstractStructureTaggerProvider>();

            var document = workspace.Documents.First();
            var textBuffer = document.GetTextBuffer();
            var tagger = tagProvider.CreateTagger<IStructureTag>(textBuffer);
            Contract.ThrowIfNull(tagger);

            using var disposable = (IDisposable)tagger;
            // The very first all to get tags will not be synchronous as this contains no #region tag
            var tags = tagger.GetTags(new NormalizedSnapshotSpanCollection(textBuffer.CurrentSnapshot.GetFullSpan()));
            Assert.Equal(0, tags.Count());
        }

        [WpfFact]
        public void TestSynchronousOutlining()
        {
            using var workspace = TestWorkspace.CreateCSharp(@"
#region x

class Program
{
}

#endregion", composition: EditorTestCompositions.EditorFeaturesWpf);
            WpfTestRunner.RequireWpfFact($"{nameof(AsynchronousTaggerTests)}.{nameof(TestSynchronousOutlining)} creates asynchronous taggers");

            var tagProvider = workspace.ExportProvider.GetExportedValue<AbstractStructureTaggerProvider>();

            var document = workspace.Documents.First();
            var textBuffer = document.GetTextBuffer();
            var tagger = tagProvider.CreateTagger<IStructureTag>(textBuffer);
            Contract.ThrowIfNull(tagger);

            using var disposable = (IDisposable)tagger;
            // The very first all to get tags will be synchronous because of the #region
            var tags = tagger.GetTags(new NormalizedSnapshotSpanCollection(textBuffer.CurrentSnapshot.GetFullSpan()));
            Assert.Equal(2, tags.Count());
        }

        private static TestTaggerEventSource CreateEventSource()
            => new TestTaggerEventSource();

        private sealed class TestTag : TextMarkerTag
        {
            public TestTag()
                : base("Test")
            {
            }
        }

        private delegate List<ITagSpan<TestTag>> Callback(SnapshotSpan span, CancellationToken cancellationToken);

        private sealed class TestTaggerProvider : AsynchronousTaggerProvider<TestTag>
        {
            private readonly Callback _callback;
            private readonly ITaggerEventSource _eventSource;

            public TestTaggerProvider(
                IThreadingContext threadingContext,
                Callback callback,
                ITaggerEventSource eventSource,
                IGlobalOptionService globalOptions,
                IAsynchronousOperationListener asyncListener)
                : base(threadingContext, globalOptions, visibilityTracker: null, asyncListener)
            {
                _callback = callback;
                _eventSource = eventSource;
            }

            protected override TaggerDelay EventChangeDelay => TaggerDelay.NearImmediate;

            protected override ITaggerEventSource CreateEventSource(ITextView? textView, ITextBuffer subjectBuffer)
                => _eventSource;

            protected override Task ProduceTagsAsync(
                TaggerContext<TestTag> context, DocumentSnapshotSpan snapshotSpan, int? caretPosition, CancellationToken cancellationToken)
            {
                var tags = _callback(snapshotSpan.SnapshotSpan, cancellationToken);
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        context.AddTag(tag);
                    }
                }

                return Task.CompletedTask;
            }
        }

        private sealed class TestTaggerEventSource : AbstractTaggerEventSource
        {
            public TestTaggerEventSource()
            {
            }

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
}
