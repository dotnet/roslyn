// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.InlayHint;

public abstract class AbstractInlayHintTests : AbstractLanguageServerProtocolTests
{
    protected AbstractInlayHintTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    private protected static async Task VerifyInlayHintAsync(TestLspServer testLspServer, bool hasTextEdits = true)
    {
        var expectedInlayHints = await GetAnnotatedLocationsAsync(testLspServer.TestWorkspace, testLspServer.GetCurrentSolution());
        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
        var textDocumentIdentifier = CreateTextDocumentIdentifier(document.GetURI());
        var text = await document.GetTextAsync(CancellationToken.None);
        var span = TextSpan.FromBounds(0, text.Length);

        var inlayHintParams = new LSP.InlayHintParams
        {
            TextDocument = textDocumentIdentifier,
            Range = ProtocolConversions.TextSpanToRange(span, text)
        };

        var actualInlayHints = await testLspServer.ExecuteRequestAsync<LSP.InlayHintParams, LSP.InlayHint[]?>(LSP.Methods.TextDocumentInlayHintName, inlayHintParams, CancellationToken.None);
        AssertEx.NotNull(actualInlayHints);

        foreach (var kvp in expectedInlayHints)
        {
            var name = kvp.Key;
            var locations = kvp.Value;

            foreach (var location in locations)
            {
                var matchingInlayHints = actualInlayHints.Where(actualInlayHints => actualInlayHints.Position == location.Range.Start);
                Assert.Single(matchingInlayHints);

                var matchingInlayHint = matchingInlayHints.Single();
                AssertEx.Equal(name, matchingInlayHint.Label.First.TrimEnd(':'));

                AssertEx.NotNull(matchingInlayHint.Kind);
                Assert.True(matchingInlayHint.PaddingRight);
                Assert.False(matchingInlayHint.PaddingLeft);
                if (hasTextEdits)
                {
                    AssertEx.NotNull(matchingInlayHint.TextEdits);
                }

                var resolvedInlayHint = await testLspServer.ExecuteRequestAsync<LSP.InlayHint, LSP.InlayHint>(LSP.Methods.InlayHintResolveName, matchingInlayHint, CancellationToken.None);
                AssertEx.NotNull(resolvedInlayHint?.ToolTip);
            }
        }
    }
}
