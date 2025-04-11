// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Formatting;

public sealed class FormatDocumentOnTypeTests : AbstractLanguageServerProtocolTests
{
    public FormatDocumentOnTypeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestFormatDocumentOnTypeAsync(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    if (true)
                        {{|type:|}
                }
            }
            """;
        var expected =
            """
            class A
            {
                void M()
                {
                    if (true)
                    {
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var characterTyped = ";";
        var locationTyped = testLspServer.GetLocations("type").Single();
        await AssertFormatDocumentOnTypeAsync(testLspServer, characterTyped, locationTyped, expected);
    }

    [Theory, CombinatorialData]
    public async Task TestFormatDocumentOnType_UseTabsAsync(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
            	void M()
            	{
            		if (true)
            			{{|type:|}
            	}
            }
            """;
        var expected =
            """
            class A
            {
            	void M()
            	{
            		if (true)
            		{
            	}
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var characterTyped = ";";
        var locationTyped = testLspServer.GetLocations("type").Single();
        await AssertFormatDocumentOnTypeAsync(testLspServer, characterTyped, locationTyped, expected, insertSpaces: false, tabSize: 4);
    }

    [Theory, CombinatorialData]
    public async Task TestFormatDocumentOnType_NewLine(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M() {
                    {|type:|}
                }
            }
            """;
        var expected =
            """
            class A
            {
                void M()
                {
                    
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var characterTyped = "\n";
        var locationTyped = testLspServer.GetLocations("type").Single();
        await AssertFormatDocumentOnTypeAsync(testLspServer, characterTyped, locationTyped, expected);
    }

    private static async Task AssertFormatDocumentOnTypeAsync(
        TestLspServer testLspServer,
        string characterTyped,
        LSP.Location locationTyped,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedText,
        bool insertSpaces = true,
        int tabSize = 4)
    {
        var documentText = await testLspServer.GetDocumentTextAsync(locationTyped.Uri);
        var results = await testLspServer.ExecuteRequestAsync<LSP.DocumentOnTypeFormattingParams, LSP.TextEdit[]>(
            LSP.Methods.TextDocumentOnTypeFormattingName,
            CreateDocumentOnTypeFormattingParams(characterTyped, locationTyped, insertSpaces, tabSize),
            CancellationToken.None);
        var actualText = ApplyTextEdits(results, documentText);
        Assert.Equal(expectedText, actualText);
    }

    private static LSP.DocumentOnTypeFormattingParams CreateDocumentOnTypeFormattingParams(
        string characterTyped,
        LSP.Location locationTyped,
        bool insertSpaces,
        int tabSize)
        => new LSP.DocumentOnTypeFormattingParams()
        {
            Position = locationTyped.Range.Start,
            Character = characterTyped,
            TextDocument = CreateTextDocumentIdentifier(locationTyped.Uri),
            Options = new LSP.FormattingOptions()
            {
                InsertSpaces = insertSpaces,
                TabSize = tabSize,
            }
        };
}
