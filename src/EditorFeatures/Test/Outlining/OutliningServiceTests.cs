// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Outlining
{
    public class OutliningServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestSimpleLambda()
        {
            var code =
@"using System.Linq;
class C
{
    static void Foo()
    {
        var q = Enumerable.Range(1, 100).Where(x =>
        {
            return x % 2 == 0;
        });
    }
}
";

            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
            {
                var spans = await GetSpansFromWorkspaceAsync(workspace);

                // ensure all 4 outlining region tags were found (usings, class, method, lambda)
                Assert.Equal(4, spans.Count);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestParenthesizedLambda()
        {
            var code =
@"using System.Linq;
class C
{
    static void Foo()
    {
        var q = Enumerable.Range(1, 100).Where((x) =>
        {
            return x % 2 == 0;
        });
    }
}
";

            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
            {
                var spans = await GetSpansFromWorkspaceAsync(workspace);

                // ensure all 4 outlining region tags were found (usings, class, method, lambda)
                Assert.Equal(4, spans.Count);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestAnonymousDelegate()
        {
            var code =
@"using System.Linq;
class C
{
    static void Foo()
    {
        var q = Enumerable.Range(1, 100).Where(delegate (int x)
        {
            return x % 2 == 0;
        });
    }
}
";

            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
            {
                var spans = await GetSpansFromWorkspaceAsync(workspace);

                // ensure all 4 outlining region tags were found (usings, class, method, anonymous delegate)
                Assert.Equal(4, spans.Count);
            }
        }

        private static async Task<IList<OutliningSpan>> GetSpansFromWorkspaceAsync(TestWorkspace workspace)
        {
            var hostDocument = workspace.Documents.First();
            var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
            var outliningService = document.Project.LanguageServices.GetService<IOutliningService>();

            return await outliningService.GetOutliningSpansAsync(document, CancellationToken.None);
        }
    }
}
