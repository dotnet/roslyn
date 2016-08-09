// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeLens
{
    public class CSharpCodeLensTests
    {
        private CodeLensReferenceService _referenceService;

        private async Task<TestWorkspace> SetupWorkspaceAsync(string content)
        {
            var workspace = await TestWorkspace.CreateCSharpAsync(content);
            _referenceService = new CodeLensReferenceService();
            return workspace;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeLens)]
        public async Task TestCount()
        {
            using (var workspace = await SetupWorkspaceAsync(@"
class A
{
    void B()
    {
        C();
    }

    void C()
    {
        D();
    }

    void D()
    {
        C();
    }
}"))
            {
                var solution = workspace.CurrentSolution;
                var documentId = workspace.Documents.First().Id;
                var document = solution.GetDocument(documentId);
                var syntaxNode = await document.GetSyntaxRootAsync();
                var iterator = syntaxNode.ChildNodes().First().ChildNodes().GetEnumerator();
                iterator.MoveNext();

                var result =
                    await
                        _referenceService.GetReferenceCountAsync(solution, documentId, iterator.Current,
                            CancellationToken.None);
                Assert.True(result.HasValue);
                Assert.Equal(0, result.Value.Count);
                Assert.False(result.Value.IsCapped);

                iterator.MoveNext();
                result =
                    await
                        _referenceService.GetReferenceCountAsync(solution, documentId, iterator.Current,
                            CancellationToken.None);
                Assert.True(result.HasValue);
                Assert.Equal(2, result.Value.Count);
                Assert.False(result.Value.IsCapped);

                iterator.MoveNext();
                result =
                    await
                        _referenceService.GetReferenceCountAsync(solution, documentId, iterator.Current,
                            CancellationToken.None);
                Assert.True(result.HasValue);
                Assert.Equal(1, result.Value.Count);
                Assert.False(result.Value.IsCapped);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeLens)]
        public async Task TestCapping()
        {
            using (var workspace = await SetupWorkspaceAsync(@"
class A
{
    void B()
    {
        C();
    }

    void C()
    {
        D();
    }

    void D()
    {
        C();
    }
}"))
            {
                var solution = workspace.CurrentSolution;
                var documentId = workspace.Documents.First().Id;
                var document = solution.GetDocument(documentId);
                var syntaxNode = await document.GetSyntaxRootAsync();
                var iterator = syntaxNode.ChildNodes().First().ChildNodes().GetEnumerator();
                iterator.MoveNext();

                var result =
                    await
                        _referenceService.GetReferenceCountAsync(solution, documentId, iterator.Current,
                            CancellationToken.None, 1);
                Assert.True(result.HasValue);
                Assert.Equal(0, result.Value.Count);
                Assert.False(result.Value.IsCapped);

                iterator.MoveNext();
                result =
                    await
                        _referenceService.GetReferenceCountAsync(solution, documentId, iterator.Current,
                            CancellationToken.None, 1);
                Assert.True(result.HasValue);
                Assert.Equal(1, result.Value.Count);
                Assert.True(result.Value.IsCapped);

                iterator.MoveNext();
                result =
                    await
                        _referenceService.GetReferenceCountAsync(solution, documentId, iterator.Current,
                            CancellationToken.None, 1);
                Assert.True(result.HasValue);
                Assert.Equal(1, result.Value.Count);
                Assert.False(result.Value.IsCapped);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeLens)]
        public async Task TestDisplay()
        {
            using (var workspace = await SetupWorkspaceAsync(@"
class A
{
    void B()
    {
        C();
    }

    void C()
    {
        D();
    }

    void D()
    {
        C();
    }
}"))
            {
                var solution = workspace.CurrentSolution;
                var documentId = workspace.Documents.First().Id;
                var document = solution.GetDocument(documentId);
                var syntaxNode = await document.GetSyntaxRootAsync();
                var iterator = syntaxNode.ChildNodes().First().ChildNodes().GetEnumerator();
                iterator.MoveNext();

                var result =
                    await
                        _referenceService.FindReferenceLocationsAsync(solution, documentId, iterator.Current,
                            CancellationToken.None);
                Assert.Equal(0, result.Count());

                iterator.MoveNext();
                result =
                    await
                        _referenceService.FindReferenceLocationsAsync(solution, documentId, iterator.Current,
                            CancellationToken.None);
                Assert.Equal(2, result.Count());

                iterator.MoveNext();
                result =
                    await
                        _referenceService.FindReferenceLocationsAsync(solution, documentId, iterator.Current,
                            CancellationToken.None);
                Assert.Equal(1, result.Count());
            }
        }
    }
}
