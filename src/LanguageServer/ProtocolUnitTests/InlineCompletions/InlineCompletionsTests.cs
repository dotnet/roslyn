// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class InlineCompletionsTests : AbstractLanguageServerProtocolTests
{
    public InlineCompletionsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override TestComposition Composition => base.Composition
        .AddParts(typeof(TestSnippetInfoService));

    [Theory, CombinatorialData]
    public Task TestSimpleSnippet(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("""
            class A
            {
                void M()
                {
                    if{|tab:|}
                }
            }
            """, """
            if (${1:true})
                    {
                        $0
                    }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task TestSnippetIgnoresCase(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("""
            class A
            {
                void M()
                {
                    If{|tab:|}
                }
            }
            """, """
            if (${1:true})
                    {
                        $0
                    }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task TestSnippetUsesOptionsFromRequest(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("""
            class A
            {
                void M()
                {
                    if{|tab:|}
                }
            }
            """, """
            if (${1:true})
              {
               $0
              }
            """, mutatingLspWorkspace, options: new LSP.FormattingOptions { TabSize = 1, InsertSpaces = true });

    [Theory, CombinatorialData]
    public Task TestSnippetWithMultipleDeclarations(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("""
            class A
            {
                void M()
                {
                    for{|tab:|}
                }
            }
            """, """
            for (int ${1:i} = 0; ${1:i} < ${2:length}; ${1:i}++)
                    {
                        $0
                    }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task TestSnippetWithSimpleTypeNameFunctionFullyQualifies(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("""
            class A
            {
                void M()
                {
                    cw{|tab:|}
                }
            }
            """, @"System.Console.WriteLine($0);", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task TestSnippetWithSimpleTypeNameFunctionWithUsing(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("""
            using System;
            class A
            {
                void M()
                {
                    cw{|tab:|}
                }
            }
            """, @"Console.WriteLine($0);", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task TestSnippetWithClassNameFunction(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("""
            class A
            {
                ctor{|tab:|}
            }
            """, """
            public A()
                {
                    $0
                }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task TestSnippetWithClassNameFunctionOutsideOfClass(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected(@"ctor{|tab:|}", """
            public ClassNamePlaceholder ()
            {
                $0
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task TestSnippetWithSwitchFunctionOnlyGeneratesDefault(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("""
            class A
            {
                void M()
                {
                    switch{|tab:|}
                }
            }
            """, """
            switch (${1:switch_on})
                    {
                        default:
                    }$0
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task TestSnippetWithNoEditableFields(bool mutatingLspWorkspace)
        => VerifyMarkupAndExpected("""
            class A
            {
                equals{|tab:|}
            }
            """, """
            // override object.Equals
                public override bool Equals(object obj)
                {
                    //       
                    // See the full list of guidelines at
                    //   http://go.microsoft.com/fwlink/?LinkID=85237  
                    // and also the guidance for operator== at
                    //   http://go.microsoft.com/fwlink/?LinkId=85238
                    //

                    if (obj == null || GetType() != obj.GetType())
                    {
                        return false;
                    }

                    // TODO: write your implementation of Equals() here
                    throw new System.NotImplementedException();
                    return base.Equals(obj);$0
                }

                // override object.GetHashCode
                public override int GetHashCode()
                {
                    // TODO: write your implementation of GetHashCode() here
                    throw new System.NotImplementedException();
                    return base.GetHashCode();
                }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public async Task TestSnippetCached(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    if{|tab:|}
                }
            }
            """;
        var expectedSnippet =
            """
            if (${1:true})
                    {
                        $0
                    }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var locationTyped = testLspServer.GetLocations("tab").Single();

        // Verify we haven't parsed snippets until asked.
        var snippetParser = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<XmlSnippetParser>();
        Assert.Equal(0, snippetParser.GetTestAccessor().GetCachedSnippetsCount());

        // Verify that the first time we ask for a snippet it gets parsed and added to the cache.
        var result = await GetInlineCompletionsAsync(testLspServer, locationTyped, new LSP.FormattingOptions { InsertSpaces = true, TabSize = 4 });
        Assert.Equal(expectedSnippet, result.Items.Single().Text);
        Assert.Equal(1, snippetParser.GetTestAccessor().GetCachedSnippetsCount());
        var firstSnippet = snippetParser.GetTestAccessor().GetCachedSnippet("if");

        // Verify that the next time we ask for the same snippet we do not parse again.
        result = await GetInlineCompletionsAsync(testLspServer, locationTyped, new LSP.FormattingOptions { InsertSpaces = true, TabSize = 4 });
        Assert.Equal(expectedSnippet, result.Items.Single().Text);
        Assert.Equal(1, snippetParser.GetTestAccessor().GetCachedSnippetsCount());
        var secondSnippet = snippetParser.GetTestAccessor().GetCachedSnippet("if");
        Assert.Same(firstSnippet, secondSnippet);
    }

    private async Task VerifyMarkupAndExpected(string markup, string expected, bool mutatingLspWorkspace, LSP.FormattingOptions? options = null)
    {
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var locationTyped = testLspServer.GetLocations("tab").Single();

        var document = testLspServer.GetDocumentAsync(locationTyped.DocumentUri);

        var result = await GetInlineCompletionsAsync(testLspServer, locationTyped, options ?? new LSP.FormattingOptions { InsertSpaces = true, TabSize = 4 });

        AssertEx.NotNull(result);
        Assert.Single(result.Items);

        var item = result.Items.Single();
        AssertEx.NotNull(item.Range);
        Assert.Equal(LSP.InsertTextFormat.Snippet, item.TextFormat);
        Assert.Equal(expected, item.Text);
    }

    private static async Task<LSP.VSInternalInlineCompletionList> GetInlineCompletionsAsync(
            TestLspServer testLspServer,
            LSP.Location locationTyped,
            LSP.FormattingOptions options)
    {
        var request = new LSP.VSInternalInlineCompletionRequest
        {
            Context = new LSP.VSInternalInlineCompletionContext
            {
                SelectedCompletionInfo = null,
                TriggerKind = LSP.VSInternalInlineCompletionTriggerKind.Explicit
            },
            Position = locationTyped.Range.Start,
            TextDocument = CreateTextDocumentIdentifier(locationTyped.DocumentUri),
            Options = options
        };

        var response = await testLspServer.ExecuteRequestAsync<LSP.VSInternalInlineCompletionRequest, LSP.VSInternalInlineCompletionList>(
            LSP.VSInternalMethods.TextDocumentInlineCompletionName, request, CancellationToken.None);
        Contract.ThrowIfNull(response);
        return response;
    }
}
