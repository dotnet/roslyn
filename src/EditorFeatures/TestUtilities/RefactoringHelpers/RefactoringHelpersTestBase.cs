// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.RefactoringHelpers
{
    [UseExportProvider]
    public abstract class RefactoringHelpersTestBase<TWorkspaceFixture> : TestBase, IClassFixture<TWorkspaceFixture>, IDisposable
        where TWorkspaceFixture : TestWorkspaceFixture, new()
    {
        protected readonly TWorkspaceFixture fixture;

        protected RefactoringHelpersTestBase(TWorkspaceFixture workspaceFixture)
        {
            this.fixture = workspaceFixture;
        }

        public override void Dispose()
        {
            this.fixture.DisposeAfterTest();
            base.Dispose();
        }

        protected Task TestAsync<TNode>(string text) where TNode : SyntaxNode => TestAsync<TNode>(text, Functions<TNode>.True);

        protected async Task TestAsync<TNode>(string text, Func<TNode, bool> predicate) where TNode : SyntaxNode
        {
            text = GetSelectionAndResultSpans(text, out var selection, out var result);
            var resultNode = await GetNodeForSelection<TNode>(text, selection, predicate).ConfigureAwait(false);

            Assert.NotNull(resultNode);
            Assert.Equal(result, resultNode.Span);
        }


        protected Task TestMissingAsync<TNode>(string text) where TNode : SyntaxNode => TestMissingAsync<TNode>(text, Functions<TNode>.True);
        protected async Task TestMissingAsync<TNode>(string text, Func<TNode, bool> predicate) where TNode : SyntaxNode
        {
            text = GetSelectionSpan(text, out var selection);

            var resultNode = await GetNodeForSelection<TNode>(text, selection, predicate).ConfigureAwait(false);
            Assert.Null(resultNode);
        }

        private static string GetSelectionSpan(string text, out TextSpan selection)
        {
            MarkupTestFile.GetSpans(text.NormalizeLineEndings(), out text, out IDictionary<string, ImmutableArray<TextSpan>> spans);

            if (spans.Count != 1 ||
                !spans.TryGetValue(string.Empty, out var selections) || selections.Length != 1)
            {
                throw new ArgumentException("Invalid missing test format: only `[|...|]` (selection) should be present.");
            }

            selection = selections.Single();
            return text;
        }

        private static string GetSelectionAndResultSpans(string text, out TextSpan selection, out TextSpan result)
        {
            MarkupTestFile.GetSpans(text.NormalizeLineEndings(), out text, out IDictionary<string, ImmutableArray<TextSpan>> spans);

            if (spans.Count != 2 ||
                !spans.TryGetValue(string.Empty, out var selections) || selections.Length != 1 ||
                !spans.TryGetValue("result", out var results) || results.Length != 1)
            {
                throw new ArgumentException("Invalid test format: both `[|...|]` (selection) and `{|result:...|}` (retrieved node span) selections are required for a test.");
            }

            selection = selections.Single();
            result = results.Single();

            return text;
        }

        private async Task<TNode> GetNodeForSelection<TNode>(string text, TextSpan selection, Func<TNode, bool> predicate) where TNode : SyntaxNode
        {
            var document = fixture.UpdateDocument(text, SourceCodeKind.Regular);
            var relevantNodes = await document.GetRelevantNodesAsync<TNode>(selection, CancellationToken.None).ConfigureAwait(false);

            return relevantNodes.FirstOrDefault(predicate);
        }
    }
}
