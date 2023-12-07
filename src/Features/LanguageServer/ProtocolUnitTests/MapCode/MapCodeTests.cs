﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.MapCode;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeMapping;

public class MapCodeTests : AbstractLanguageServerProtocolTests
{
    [ExportLanguageService(typeof(IMapCodeService), language: LanguageNames.CSharp, layer: ServiceLayer.Test), Shared, PartNotDiscoverable]
    private class TestMapCodeService : IMapCodeService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestMapCodeService()
        {
        }

        public async Task<ImmutableArray<TextChange>?> MapCodeAsync(
            Document document, ImmutableArray<string> contents, ImmutableArray<(Document, TextSpan)> focusLocations, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken);
            var newText = text.Replace(focusLocations.Single().Item2, contents.Single());
            return newText.GetTextChanges(text).ToImmutableArray();
        }
    }

    protected override TestComposition Composition => FeaturesLspComposition
        .AddParts(typeof(TestMapCodeService));

    public MapCodeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
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
                    static void Main(string[] args)
                    {
                        {|range:|}
                    }
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
        var mapCodeParams = new LSP.MapCodeParams
            (
                Mappings:
                [
                    new MapCodeMapping
                    (
                        TextDocument: CreateTextDocumentIdentifier(documentUri),
                        Contents: [codeBlock],
                        FocusLocations: [ranges]
                    )
                ],
                Updates: null
            );

        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

        var results = await testLspServer.ExecuteRequestAsync<LSP.MapCodeParams, LSP.WorkspaceEdit>(MapCodeHandler.WorkspaceMapCodeName, mapCodeParams, CancellationToken.None);
        AssertEx.NotNull(results);

        TextEdit[] edits;
        if (supportDocumentChanges)
        {
            Assert.Null(results.Changes);
            Assert.NotNull(results.DocumentChanges);

            var textDocumentEdits = results.DocumentChanges!.Value.First.Single();
            Assert.Equal(textDocumentEdits.TextDocument.Uri, mapCodeParams.Mappings.Single().TextDocument!.Uri);

            edits = textDocumentEdits.Edits;
        }
        else
        {
            Assert.NotNull(results.Changes);
            Assert.Null(results.DocumentChanges);

            Assert.True(results.Changes!.TryGetValue(ProtocolConversions.GetDocumentFilePathFromUri(documentUri), out edits));
        }

        var documentText = await document.GetTextAsync();
        var actualText = ApplyTextEdits(edits, documentText);
        Assert.Equal(expected, actualText);
    }
}
