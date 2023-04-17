// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion;
public class CompletionFeaturesTests : AbstractLanguageServerProtocolTests
{
    protected override TestComposition Composition => FeaturesLspComposition;

    public CompletionFeaturesTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1801810")]
    public async Task TestDoesNotThrowInComplexEditWhenDisplayTextShorterThanDefaultSpanAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"
using System;
using System.Text;

public class A
{
    public int M()
    {
        return{|caret:|}
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new LSP.ClientCapabilities());
        var caret = testLspServer.GetLocations("caret").Single();
        var completionParams = new LSP.CompletionParams()
        {
            TextDocument = CreateTextDocumentIdentifier(caret.Uri),
            Position = caret.Range.Start,
            Context = new LSP.CompletionContext()
            {
                TriggerKind = LSP.CompletionTriggerKind.Invoked,
            }
        };

        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

        var results = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName, completionParams, CancellationToken.None);
        AssertEx.NotNull(results);
        Assert.NotEmpty(results.Items);
    }
}
