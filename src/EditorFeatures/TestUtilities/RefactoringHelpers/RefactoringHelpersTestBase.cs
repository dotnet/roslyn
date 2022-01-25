// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.RefactoringHelpers
{
    [UseExportProvider]
    public abstract class RefactoringHelpersTestBase<TWorkspaceFixture> : TestBase
        where TWorkspaceFixture : TestWorkspaceFixture, new()
    {
        private readonly TestFixtureHelper<TWorkspaceFixture> _fixtureHelper = new();

        private protected ReferenceCountedDisposable<TWorkspaceFixture> GetOrCreateWorkspaceFixture()
            => _fixtureHelper.GetOrCreateFixture();

        protected Task TestAsync<TNode>(string text, bool allowEmptyNodes = false) where TNode : SyntaxNode
            => TestAsync(text, Functions<TNode>.True, allowEmptyNodes);

        protected async Task TestAsync<TNode>(string text, Func<TNode, bool> predicate, bool allowEmptyNodes = false) where TNode : SyntaxNode
        {
            text = GetSelectionAndResultSpans(text, out var selection, out var result);
            var resultNode = await GetNodeForSelectionAsync(text, selection, predicate, allowEmptyNodes).ConfigureAwait(false);

            Assert.NotNull(resultNode);
            Assert.Equal(result, resultNode.Span);
        }

        protected async Task TestUnderselectedAsync<TNode>(string text) where TNode : SyntaxNode
        {
            text = GetSelectionSpan(text, out var selection);
            var resultNode = await GetNodeForSelectionAsync<TNode>(text, selection, Functions<TNode>.True).ConfigureAwait(false);

            Assert.NotNull(resultNode);
            Assert.True(CodeRefactoringHelpers.IsNodeUnderselected(resultNode, selection));
        }

        protected async Task TestNotUnderselectedAsync<TNode>(string text) where TNode : SyntaxNode
        {
            text = GetSelectionAndResultSpans(text, out var selection, out var result);
            var resultNode = await GetNodeForSelectionAsync<TNode>(text, selection, Functions<TNode>.True).ConfigureAwait(false);

            Assert.Equal(result, resultNode.Span);
            Assert.False(CodeRefactoringHelpers.IsNodeUnderselected(resultNode, selection));
        }

        protected Task TestMissingAsync<TNode>(string text, bool allowEmptyNodes = false) where TNode : SyntaxNode
            => TestMissingAsync(text, Functions<TNode>.True, allowEmptyNodes);

        protected async Task TestMissingAsync<TNode>(string text, Func<TNode, bool> predicate, bool allowEmptyNodes = false) where TNode : SyntaxNode
        {
            text = GetSelectionSpan(text, out var selection);

            var resultNode = await GetNodeForSelectionAsync<TNode>(text, selection, predicate, allowEmptyNodes).ConfigureAwait(false);
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

        private async Task<TNode> GetNodeForSelectionAsync<TNode>(string text, TextSpan selection, Func<TNode, bool> predicate, bool allowEmptyNodes = false) where TNode : SyntaxNode
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            var document = workspaceFixture.Target.UpdateDocument(text, SourceCodeKind.Regular);
            var relevantNodes = await document.GetRelevantNodesAsync<TNode>(selection, allowEmptyNodes, CancellationToken.None).ConfigureAwait(false);

            return relevantNodes.FirstOrDefault(predicate);
        }
    }
}
