// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    [Trait(Traits.Feature, Traits.Features.Outlining)]
    public class BlockStructureServiceTests
    {
        [Fact]
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

        [Fact]
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

        [Fact]
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
            var options = BlockStructureOptions.Default;

            var structure = await outliningService.GetBlockStructureAsync(document, options, CancellationToken.None);
            return structure.Spans;
        }
    }
}
