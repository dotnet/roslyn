// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure
{
    [UseExportProvider]
    public class BlockStructureServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestSimpleLambda()
        {
            var code =
@"using System.Linq;
class C
{
    static void Goo()
    {
        var q = Enumerable.Range(1, 100).Where(x =>
        {
            return x % 2 == 0;
        });
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(code);
            var spans = await GetSpansFromWorkspaceAsync(workspace);

            // ensure all 4 outlining region tags were found (usings, class, method, lambda)
            Assert.Equal(4, spans.Length);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestParenthesizedLambda()
        {
            var code =
@"using System.Linq;
class C
{
    static void Goo()
    {
        var q = Enumerable.Range(1, 100).Where((x) =>
        {
            return x % 2 == 0;
        });
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(code);
            var spans = await GetSpansFromWorkspaceAsync(workspace);

            // ensure all 4 outlining region tags were found (usings, class, method, lambda)
            Assert.Equal(4, spans.Length);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestAnonymousDelegate()
        {
            var code =
@"using System.Linq;
class C
{
    static void Goo()
    {
        var q = Enumerable.Range(1, 100).Where(delegate (int x)
        {
            return x % 2 == 0;
        });
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(code);
            var spans = await GetSpansFromWorkspaceAsync(workspace);

            // ensure all 4 outlining region tags were found (usings, class, method, anonymous delegate)
            Assert.Equal(4, spans.Length);
        }

        private static async Task<ImmutableArray<BlockSpan>> GetSpansFromWorkspaceAsync(
            TestWorkspace workspace)
        {
            var hostDocument = workspace.Documents.First();
            var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
            var outliningService = document.GetLanguageService<BlockStructureService>();

            var structure = await outliningService.GetBlockStructureAsync(document, CancellationToken.None);
            return structure.Spans;
        }
    }
}
