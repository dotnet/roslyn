// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Formatting
{
    public class FormatDocumentTests : AbstractLanguageServerProtocolTests
    {
        public FormatDocumentTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task TestFormatDocumentAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
void M()
{
            int i = 1;{|caret:|}
    }
}";
            var expected =
@"class A
{
    void M()
    {
        int i = 1;
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var documentURI = testLspServer.GetLocations("caret").Single().Uri;
            var documentText = await testLspServer.GetCurrentSolution().GetDocuments(documentURI).Single().GetTextAsync();

            var results = await RunFormatDocumentAsync(testLspServer, documentURI);
            var actualText = ApplyTextEdits(results, documentText);
            AssertEx.EqualOrDiff(expected, actualText);
        }

        [Theory, CombinatorialData]
        public async Task TestFormatDocumentWithOrganizeImports1Async(bool mutatingLspWorkspace)
        {
            var markup =
                """
                using System.Collections;
                using System;

                class A{|caret:|}
                {
                }
                """;
            var expected =
                """
                using System;
                using System.Collections;

                class A
                {
                }
                """;

            var options = new InitializationOptions
            {
                OptionUpdater = globalOptions =>
                {
                    globalOptions.SetGlobalOption(LspOptionsStorage.LspFormattingSortImports, LanguageNames.CSharp, true);
                },
            };

            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, options);
            var documentURI = testLspServer.GetLocations("caret").Single().Uri;
            var documentText = await testLspServer.GetCurrentSolution().GetDocuments(documentURI).Single().GetTextAsync();

            var results = await RunFormatDocumentAsync(testLspServer, documentURI);
            var actualText = ApplyTextEdits(results, documentText);
            AssertEx.EqualOrDiff(expected, actualText);
        }

        [Theory, CombinatorialData]
        public async Task TestFormatDocumentWithOrganizeImports2Async(bool mutatingLspWorkspace)
        {
            var markup =
                """
                using   System.Collections;
                using  System;

                class A{|caret:|}
                {
                }
                """;
            var expected =
                """
                using System;
                using System.Collections;

                class A
                {
                }
                """;

            var options = new InitializationOptions
            {
                OptionUpdater = globalOptions =>
                {
                    globalOptions.SetGlobalOption(LspOptionsStorage.LspFormattingSortImports, LanguageNames.CSharp, true);
                },
            };

            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, options);
            var documentURI = testLspServer.GetLocations("caret").Single().Uri;
            var documentText = await testLspServer.GetCurrentSolution().GetDocuments(documentURI).Single().GetTextAsync();

            var results = await RunFormatDocumentAsync(testLspServer, documentURI);
            var actualText = ApplyTextEdits(results, documentText);
            AssertEx.EqualOrDiff(expected, actualText);
        }

        [Theory, CombinatorialData]
        public async Task TestFormatDocumentWithOrganizeImports3Async(bool mutatingLspWorkspace)
        {
            var markup =
                """
                using  System;
                using   System.Collections;

                class A{|caret:|}
                {
                }
                """;
            var expected =
                """
                using System;
                using System.Collections;

                class A
                {
                }
                """;

            var options = new InitializationOptions
            {
                OptionUpdater = globalOptions =>
                {
                    globalOptions.SetGlobalOption(LspOptionsStorage.LspFormattingSortImports, LanguageNames.CSharp, true);
                },
            };

            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, options);
            var documentURI = testLspServer.GetLocations("caret").Single().Uri;
            var documentText = await testLspServer.GetCurrentSolution().GetDocuments(documentURI).Single().GetTextAsync();

            var results = await RunFormatDocumentAsync(testLspServer, documentURI);
            var actualText = ApplyTextEdits(results, documentText);
            AssertEx.EqualOrDiff(expected, actualText);
        }

        [Theory, CombinatorialData]
        public async Task TestFormatDocumentWithOrganizeImports4Async(bool mutatingLspWorkspace)
        {
            var markup =
                """
                using System;
                using System.Collections;

                class  A{|caret:|}
                {
                }
                """;
            var expected =
                """
                using System;
                using System.Collections;

                class A
                {
                }
                """;

            var options = new InitializationOptions
            {
                OptionUpdater = globalOptions =>
                {
                    globalOptions.SetGlobalOption(LspOptionsStorage.LspFormattingSortImports, LanguageNames.CSharp, true);
                },
            };

            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, options);
            var documentURI = testLspServer.GetLocations("caret").Single().Uri;
            var documentText = await testLspServer.GetCurrentSolution().GetDocuments(documentURI).Single().GetTextAsync();

            var results = await RunFormatDocumentAsync(testLspServer, documentURI);
            var actualText = ApplyTextEdits(results, documentText);
            AssertEx.EqualOrDiff(expected, actualText);
        }

        [Theory, CombinatorialData]
        public async Task TestFormatDocumentWithoutOrganizeImportsAsync(bool mutatingLspWorkspace)
        {
            var markup =
                """
                using System.Collections;
                using System;

                class A{|caret:|}
                {
                }
                """;
            var expected =
                """
                using System.Collections;
                using System;

                class A
                {
                }
                """;

            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var documentURI = testLspServer.GetLocations("caret").Single().Uri;
            var documentText = await testLspServer.GetCurrentSolution().GetDocuments(documentURI).Single().GetTextAsync();

            var results = await RunFormatDocumentAsync(testLspServer, documentURI);
            var actualText = ApplyTextEdits(results, documentText);
            AssertEx.EqualOrDiff(expected, actualText);
        }

        [Theory, CombinatorialData]
        public async Task TestFormatDocument_UseTabsAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
void M()
{
			int i = 1;{|caret:|}
    }
}";
            var expected =
@"class A
{
	void M()
	{
		int i = 1;
	}
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var documentURI = testLspServer.GetLocations("caret").Single().Uri;
            var documentText = await testLspServer.GetCurrentSolution().GetDocuments(documentURI).Single().GetTextAsync();

            var results = await RunFormatDocumentAsync(testLspServer, documentURI, insertSpaces: false, tabSize: 4);
            var actualText = ApplyTextEdits(results, documentText);
            AssertEx.EqualOrDiff(expected, actualText);
        }

        [Theory, CombinatorialData]
        public async Task TestFormatDocument_ModifyTabIndentSizeAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
void M()
{
			int i = 1;{|caret:|}
    }
}";
            var expected =
@"class A
{
  void M()
  {
    int i = 1;
  }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var documentURI = testLspServer.GetLocations("caret").Single().Uri;
            var documentText = await testLspServer.GetCurrentSolution().GetDocuments(documentURI).Single().GetTextAsync();

            var results = await RunFormatDocumentAsync(testLspServer, documentURI, insertSpaces: true, tabSize: 2);
            var actualText = ApplyTextEdits(results, documentText);
            AssertEx.EqualOrDiff(expected, actualText);
        }

        private static async Task<LSP.TextEdit[]?> RunFormatDocumentAsync(
            TestLspServer testLspServer,
            Uri uri,
            bool insertSpaces = true,
            int tabSize = 4)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.DocumentFormattingParams, LSP.TextEdit[]>(LSP.Methods.TextDocumentFormattingName,
                CreateDocumentFormattingParams(uri, insertSpaces, tabSize), CancellationToken.None);
        }

        private static LSP.DocumentFormattingParams CreateDocumentFormattingParams(Uri uri, bool insertSpaces, int tabSize)
            => new LSP.DocumentFormattingParams()
            {
                TextDocument = CreateTextDocumentIdentifier(uri),
                Options = new LSP.FormattingOptions()
                {
                    InsertSpaces = insertSpaces,
                    TabSize = tabSize,
                }
            };
    }
}
