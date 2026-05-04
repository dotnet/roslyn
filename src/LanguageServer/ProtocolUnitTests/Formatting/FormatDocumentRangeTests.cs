// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Formatting;

public sealed class FormatDocumentRangeTests : AbstractLanguageServerProtocolTests
{
    public FormatDocumentRangeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestFormatDocumentRangeAsync(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
            {|format:void|} M()
            {
                        int i = 1;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var rangeToFormat = testLspServer.GetLocations("format").Single();
        var documentText = await testLspServer.GetDocumentTextAsync(rangeToFormat.DocumentUri);

        var results = await RunFormatDocumentRangeAsync(testLspServer, rangeToFormat);
        var actualText = ApplyTextEdits(results, documentText);
        Assert.Equal("""
            class A
            {
                void M()
            {
                        int i = 1;
                }
            }
            """, actualText);
    }

    [Theory, CombinatorialData]
    public async Task TestFormatDocumentRange_UseTabsAsync(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
            {|format:void|} M()
            {
            			int i = 1;
            	}
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var rangeToFormat = testLspServer.GetLocations("format").Single();
        var documentText = await testLspServer.GetDocumentTextAsync(rangeToFormat.DocumentUri);

        var results = await RunFormatDocumentRangeAsync(testLspServer, rangeToFormat, insertSpaces: false, tabSize: 4);
        var actualText = ApplyTextEdits(results, documentText);
        Assert.Equal("""
            class A
            {
            	void M()
            {
            			int i = 1;
            	}
            }
            """, actualText);
    }

    private static async Task<LSP.TextEdit[]> RunFormatDocumentRangeAsync(
        TestLspServer testLspServer,
        LSP.Location location,
        bool insertSpaces = true,
        int tabSize = 4)
    {
        return await testLspServer.ExecuteRequestAsync<LSP.DocumentRangeFormattingParams, LSP.TextEdit[]>(
            LSP.Methods.TextDocumentRangeFormattingName,
            CreateDocumentRangeFormattingParams(location, insertSpaces, tabSize),
            CancellationToken.None);
    }

    private static LSP.DocumentRangeFormattingParams CreateDocumentRangeFormattingParams(
        LSP.Location location,
        bool insertSpaces,
        int tabSize)
        => new()
        {
            Range = location.Range,
            TextDocument = CreateTextDocumentIdentifier(location.DocumentUri),
            Options = new LSP.FormattingOptions()
            {
                InsertSpaces = insertSpaces,
                TabSize = tabSize
            }
        };
}
