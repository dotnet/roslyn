// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeMapper;

public class CodeMapperTests : AbstractLanguageServerProtocolTests
{
    protected override TestComposition Composition => FeaturesLspComposition;

    public CodeMapperTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    private static ClientCapabilities CreateClientCapabilities(bool supportDocumentChanges)
        => new LSP.ClientCapabilities
        {
            Workspace = new LSP.WorkspaceClientCapabilities
            {
                WorkspaceEdit = new LSP.WorkspaceEditSetting
                {
                    DocumentChanges = supportDocumentChanges,
                }
            }
        };

    [Theory, CombinatorialData]
    public async Task InsertConsoleWriteLineArgsInMain(bool mutatingLspWorkspace, bool supportDocumentChanges)
    {
        var code = """
            namespace ConsoleApp1
            {
                class Program
                {
                    {|range:static void Main(string[] args)
                    {
            
                    }|}
                }
            }
            """;

        var codeBlock = @"Console.WriteLine(string.Join("", "", args));";

        var expected = """
            namespace ConsoleApp1
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Console.WriteLine(string.Join(", ", args));
                    }
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(code, mutatingLspWorkspace, CreateClientCapabilities(supportDocumentChanges));
        var ranges = testLspServer.GetLocations("range").ToArray();
        var documentUri = ranges.Single().Uri;
        var mapCodeParams = new LSP.MapCodeParams()
        {
            TextDocument = CreateTextDocumentIdentifier(documentUri),
            Contents = new[] { codeBlock },
            FocusLocations = ranges
        };

        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

        var results = await testLspServer.ExecuteRequestAsync<LSP.MapCodeParams, LSP.WorkspaceEdit>(LSP.MapperMethods.TextDocumentMapCodeName, mapCodeParams, CancellationToken.None);
        AssertEx.NotNull(results);

        TextEdit[] edits;
        if (supportDocumentChanges)
        {
            Assert.Null(results.Changes);
            Assert.NotNull(results.DocumentChanges);

            var textDocumentEdits = results.DocumentChanges.Value.First.Single();
            Assert.Equal(textDocumentEdits.TextDocument.Uri, mapCodeParams.TextDocument.Uri);

            edits = textDocumentEdits.Edits;
        }
        else
        {
            Assert.NotNull(results.Changes);
            Assert.Null(results.DocumentChanges);

            Assert.True(results.Changes.TryGetValue(documentUri.AbsolutePath, out edits));

        }

        var documentText = await document.GetTextAsync();
        var actualText = ApplyTextEdits(edits, documentText);
        Assert.Equal(expected, actualText);

    }
}
