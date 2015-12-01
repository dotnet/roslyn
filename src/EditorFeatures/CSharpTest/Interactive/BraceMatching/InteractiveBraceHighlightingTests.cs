﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BraceHighlighting
{
    public class InteractiveBraceHighlightingTests
    {
        private IEnumerable<T> Enumerable<T>(params T[] array)
        {
            return array;
        }

        private IEnumerable<ITagSpan<BraceHighlightTag>> ProduceTags(
            TestWorkspace workspace,
            ITextBuffer buffer,
            int position)
        {
            var view = new Mock<ITextView>();
            var producer = new BraceHighlightingViewTaggerProvider(
                workspace.GetService<IBraceMatchingService>(),
                workspace.GetService<IForegroundNotificationService>(),
                AggregateAsynchronousOperationListener.EmptyListeners);

            var context = new TaggerContext<BraceHighlightTag>(
                buffer.CurrentSnapshot.GetRelatedDocumentsWithChanges().FirstOrDefault(),
                buffer.CurrentSnapshot, new SnapshotPoint(buffer.CurrentSnapshot, position));
            producer.ProduceTagsAsync_ForTestingPurposesOnly(context).Wait();

            return context.tagSpans;
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public async Task TestCurlies()
        {
            var code = new string[] { "public class C {", "} " };
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(code, parseOptions: Options.Script))
            {
                var buffer = workspace.Documents.First().GetTextBuffer();

                // Before open curly
                var result = ProduceTags(workspace, buffer, 14);
                Assert.True(result.IsEmpty());

                // At open curly
                result = ProduceTags(workspace, buffer, 15);
                Assert.True(result.Select(ts => ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(15, 16), Span.FromBounds(18, 19))));

                // After open curly
                result = ProduceTags(workspace, buffer, 16);
                Assert.True(result.IsEmpty());

                // At close curly
                result = ProduceTags(workspace, buffer, 18);
                Assert.True(result.IsEmpty());

                // After close curly
                result = ProduceTags(workspace, buffer, 19);
                Assert.True(result.Select(ts => ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(15, 16), Span.FromBounds(18, 19))));
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public async Task TestTouchingItems()
        {
            var code = new string[] { "public class C {", "  public void Foo(){}", "}" };
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(code, Options.Script))
            {
                var buffer = workspace.Documents.First().GetTextBuffer();

                // Before open curly
                var result = ProduceTags(workspace, buffer, 35);
                Assert.True(result.Select(ts => ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(35, 36), Span.FromBounds(36, 37))));

                // At open curly
                result = ProduceTags(workspace, buffer, 36);
                Assert.True(result.IsEmpty());

                // After open curly
                result = ProduceTags(workspace, buffer, 37);
                Assert.True(result.Select(ts => ts.Span.Span).SetEquals(
                    Enumerable(Span.FromBounds(35, 36), Span.FromBounds(36, 37), Span.FromBounds(37, 38), Span.FromBounds(38, 39))));

                // At close curly
                result = ProduceTags(workspace, buffer, 38);
                Assert.True(result.IsEmpty());

                // After close curly
                result = ProduceTags(workspace, buffer, 39);
                Enumerable(Span.FromBounds(37, 38), Span.FromBounds(38, 39));
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public async Task TestAngles()
        {
            var code = new string[] { "/// <summary>Foo</summary>", "public class C<T> {", "  void Foo() {", "    bool a = b < c;", "    bool d = e > f;", "  }", "} " };
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(code, parseOptions: Options.Script))
            {
                var buffer = workspace.Documents.First().GetTextBuffer();

                // Before open angle of generic
                var result = ProduceTags(workspace, buffer, 42);
                Assert.True(result.Select(ts => ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(42, 43), Span.FromBounds(44, 45))));

                // After close angle of generic
                result = ProduceTags(workspace, buffer, 45);
                Assert.True(result.Select(ts => ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(42, 43), Span.FromBounds(44, 45))));

                Action<int, char> assertNoTags = (position, expectedChar) =>
                {
                    Assert.Equal(expectedChar, buffer.CurrentSnapshot[position]);
                    result = ProduceTags(workspace, buffer, position);
                    Assert.True(result.IsEmpty());
                    result = ProduceTags(workspace, buffer, position + 1);
                    Assert.True(result.IsEmpty());
                };

                // Doesn't highlight angles of XML doc comments
                var xmlTagStartPosition = 4;
                assertNoTags(xmlTagStartPosition, '<');

                var xmlTagEndPosition = 12;
                assertNoTags(xmlTagEndPosition, '>');

                var xmlEndTagStartPosition = 16;
                assertNoTags(xmlEndTagStartPosition, '<');

                var xmlEndTagEndPosition = 25;
                assertNoTags(xmlEndTagEndPosition, '>');

                // Doesn't highlight operators
                var openAnglePosition = 15 + buffer.CurrentSnapshot.GetLineFromLineNumber(3).Start;
                assertNoTags(openAnglePosition, '<');

                var closeAnglePosition = 15 + buffer.CurrentSnapshot.GetLineFromLineNumber(4).Start;
                assertNoTags(closeAnglePosition, '>');
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)]
        public async Task TestSwitch()
        {
            var code = @"
class C
{
    void M(int variable)
    {
        switch (variable)
        {
            case 0:
                break;
        }
    }
} ";
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(code, parseOptions: Options.Script))
            {
                var buffer = workspace.Documents.First().GetTextBuffer();

                // At switch open paren
                var result = ProduceTags(workspace, buffer, 62);
                AssertEx.Equal(Enumerable(new Span(62, 1), new Span(71, 1)), result.Select(ts => ts.Span.Span).OrderBy(s => s.Start));

                // After switch open paren
                result = ProduceTags(workspace, buffer, 83);
                Assert.True(result.IsEmpty());

                // At switch close paren
                result = ProduceTags(workspace, buffer, 71);
                Assert.True(result.IsEmpty());

                // After switch close paren
                result = ProduceTags(workspace, buffer, 72);
                AssertEx.Equal(Enumerable(new Span(62, 1), new Span(71, 1)), result.Select(ts => ts.Span.Span).OrderBy(s => s.Start));

                // At switch open curly
                result = ProduceTags(workspace, buffer, 82);
                AssertEx.Equal(Enumerable(new Span(82, 1), new Span(138, 1)), result.Select(ts => ts.Span.Span).OrderBy(s => s.Start));

                // After switch open curly
                result = ProduceTags(workspace, buffer, 83);
                Assert.True(result.IsEmpty());

                // At switch close curly
                result = ProduceTags(workspace, buffer, 138);
                Assert.True(result.IsEmpty());

                // After switch close curly
                result = ProduceTags(workspace, buffer, 139);
                AssertEx.Equal(Enumerable(new Span(82, 1), new Span(138, 1)), result.Select(ts => ts.Span.Span).OrderBy(s => s.Start));
            }
        }
    }
}
