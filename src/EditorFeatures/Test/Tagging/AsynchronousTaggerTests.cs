﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
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

            var notificationService = workspace.GetService<IForegroundNotificationService>();

            var eventSource = CreateEventSource();
            var taggerProvider = new TestTaggerProvider(
                workspace.ExportProvider.GetExportedValue<IThreadingContext>(),
                tagProducer,
                eventSource,
                workspace,
                asyncListener,
                notificationService);

            var document = workspace.Documents.First();
            var textBuffer = document.GetTextBuffer();
            var snapshot = textBuffer.CurrentSnapshot;
            var tagger = taggerProvider.CreateTagger<TestTag>(textBuffer);

            using var disposable = (IDisposable)tagger;
            var spans = Enumerable.Range(0, 101).Select(i => new Span(i * 4, 1));
            var snapshotSpans = new NormalizedSnapshotSpanCollection(snapshot, spans);

            eventSource.SendUpdateEvent();

            await asyncListener.ExpeditedWaitAsync();

            var tags = tagger.GetTags(snapshotSpans);

            Assert.Equal(1, tags.Count());
        }

        [WpfFact]
        public void TestSynchronousOutlining()
        {
            using var workspace = TestWorkspace.CreateCSharp("class Program {\r\n\r\n}");
            WpfTestRunner.RequireWpfFact($"{nameof(AsynchronousTaggerTests)}.{nameof(TestSynchronousOutlining)} creates asynchronous taggers");

            var tagProvider = new VisualStudio14StructureTaggerProvider(
                workspace.ExportProvider.GetExportedValue<IThreadingContext>(),
                workspace.GetService<IForegroundNotificationService>(),
                workspace.GetService<ITextEditorFactoryService>(),
                workspace.GetService<IEditorOptionsFactoryService>(),
                workspace.GetService<IProjectionBufferFactoryService>(),
                workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>());

            var document = workspace.Documents.First();
            var textBuffer = document.GetTextBuffer();
            var tagger = tagProvider.CreateTagger<IOutliningRegionTag>(textBuffer);

            using var disposable = (IDisposable)tagger;
            // The very first all to get tags should return the single outlining span.
            var tags = tagger.GetAllTags(new NormalizedSnapshotSpanCollection(textBuffer.CurrentSnapshot.GetFullSpan()), CancellationToken.None);
            Assert.Equal(1, tags.Count());
        }

        private static TestTaggerEventSource CreateEventSource()
        {
            return new TestTaggerEventSource();
        }

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
            private readonly Workspace _workspace;
            private readonly bool _disableCancellation;

            public TestTaggerProvider(
                IThreadingContext threadingContext,
                Callback callback,
                ITaggerEventSource eventSource,
                Workspace workspace,
                IAsynchronousOperationListener asyncListener,
                IForegroundNotificationService notificationService,
                bool disableCancellation = false)
                    : base(threadingContext, asyncListener, notificationService)
            {
                _callback = callback;
                _eventSource = eventSource;
                _workspace = workspace;
                _disableCancellation = disableCancellation;
            }

            protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
            {
                return _eventSource;
            }

            protected override Task ProduceTagsAsync(TaggerContext<TestTag> context, DocumentSnapshotSpan snapshotSpan, int? caretPosition)
            {
                var tags = _callback(snapshotSpan.SnapshotSpan, context.CancellationToken);
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
                : base(delay: TaggerDelay.NearImmediate)
            {
            }

            public void SendUpdateEvent()
            {
                this.RaiseChanged();
            }

            public override void Connect()
            {
            }

            public override void Disconnect()
            {
            }
        }
    }
}
