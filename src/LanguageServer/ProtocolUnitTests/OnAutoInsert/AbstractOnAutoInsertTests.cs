// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.OnAutoInsert;

[Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
public abstract class AbstractOnAutoInsertTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    private protected async Task VerifyCSharpMarkupAndExpected(
        string characterTyped,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expected,
        bool mutatingLspWorkspace,
        bool insertSpaces = true,
        int tabSize = 4,
        string languageName = LanguageNames.CSharp,
        WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.AlwaysActiveVSLspServer,
        bool useVSCapabilities = true)
    {
        var capbilities = GetCapabilities(useVSCapabilities);
        Task<TestLspServer> testLspServerTask;
        if (languageName == LanguageNames.CSharp)
        {
            testLspServerTask = CreateTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions { ClientCapabilities = capbilities, ServerKind = serverKind });
        }
        else if (languageName == LanguageNames.VisualBasic)
        {
            testLspServerTask = CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions { ClientCapabilities = capbilities, ServerKind = serverKind });
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(languageName);
        }

        await using var testLspServer = await testLspServerTask;
        var locationTyped = testLspServer.GetLocations("type").Single();

        var document = await testLspServer.GetDocumentAsync(locationTyped.DocumentUri);
        var documentText = await document.GetTextAsync();

        var result = await RunOnAutoInsertAsync(testLspServer, characterTyped, locationTyped, insertSpaces, tabSize);

        AssertEx.NotNull(result);
        Assert.Equal(InsertTextFormat.Snippet, result.TextEditFormat);
        var actualText = ApplyTextEdits([result.TextEdit], documentText);
        Assert.Equal(expected, actualText);
    }

    private protected async Task VerifyNoResult(
        string characterTyped,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        bool mutatingLspWorkspace,
        bool insertSpaces = true,
        int tabSize = 4,
        WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.AlwaysActiveVSLspServer,
        bool useVSCapabilities = true)
    {
        var initilizationOptions = new InitializationOptions
        {
            ClientCapabilities = GetCapabilities(useVSCapabilities),
            ServerKind = serverKind
        };
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, initilizationOptions);
        var locationTyped = testLspServer.GetLocations("type").Single();
        var documentText = await (await testLspServer.GetDocumentAsync(locationTyped.DocumentUri)).GetTextAsync();

        var result = await RunOnAutoInsertAsync(testLspServer, characterTyped, locationTyped, insertSpaces, tabSize);

        Assert.Null(result);
    }

    private protected static async Task<LSP.VSInternalDocumentOnAutoInsertResponseItem?> RunOnAutoInsertAsync(
        TestLspServer testLspServer,
        string characterTyped,
        LSP.Location locationTyped,
        bool insertSpaces,
        int tabSize)
    {
        return await testLspServer.ExecuteRequestAsync<LSP.VSInternalDocumentOnAutoInsertParams, LSP.VSInternalDocumentOnAutoInsertResponseItem?>(VSInternalMethods.OnAutoInsertName,
            CreateDocumentOnAutoInsertParams(characterTyped, locationTyped, insertSpaces, tabSize), CancellationToken.None);
    }

    private protected static LSP.VSInternalDocumentOnAutoInsertParams CreateDocumentOnAutoInsertParams(
        string characterTyped,
        LSP.Location locationTyped,
        bool insertSpaces,
        int tabSize)
        => new()
        {
            Position = locationTyped.Range.Start,
            Character = characterTyped,
            TextDocument = CreateTextDocumentIdentifier(locationTyped.DocumentUri),
            Options = new LSP.FormattingOptions
            {
                InsertSpaces = insertSpaces,
                TabSize = tabSize
            }
        };
}
