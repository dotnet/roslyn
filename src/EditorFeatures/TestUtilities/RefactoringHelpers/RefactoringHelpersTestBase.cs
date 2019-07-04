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

        protected async Task TestAsync<TNode>(string text) where TNode : SyntaxNode
        {
            MarkupTestFile.GetSpans(text.NormalizeLineEndings(), out text, out IDictionary<string, ImmutableArray<TextSpan>> spans);

            if (spans.Count != 2 ||
                !spans.TryGetValue(string.Empty, out var selection) || selection.Length != 1 ||
                !spans.TryGetValue("result", out var result) || result.Length != 1)
            {
                throw new ArgumentException("Invalid test format: both `[|...|]` (selection) and `{|result:...|}` (retrieved node span) selections are required for a test.");
            }

            var document = fixture.UpdateDocument(text, SourceCodeKind.Regular);
            var service = document.GetLanguageService<IRefactoringHelpersService>();

            var resultNode = await service.TryGetSelectedNodeAsync<TNode>(document, selection.First(), CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(resultNode);
            Assert.Equal(resultNode.Span, result.First());
        }

        protected async Task TestMissingAsync<TNode>(string text) where TNode : SyntaxNode
        {
            MarkupTestFile.GetSpans(text.NormalizeLineEndings(), out text, out IDictionary<string, ImmutableArray<TextSpan>> spans);

            if (spans.Count != 1 ||
                !spans.TryGetValue(string.Empty, out var selection) || selection.Length != 1)
            {
                throw new ArgumentException("Invalid missing test format: only `[|...|]` (selection) should be present.");
            }

            var document = fixture.UpdateDocument(text, SourceCodeKind.Regular);
            var service = document.GetLanguageService<IRefactoringHelpersService>();

            var resultNode = await service.TryGetSelectedNodeAsync<TNode>(document, selection.First(), CancellationToken.None).ConfigureAwait(false);

            Assert.Null(resultNode);
        }
    }
}
