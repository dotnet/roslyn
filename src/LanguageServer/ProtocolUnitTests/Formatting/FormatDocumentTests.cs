// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Formatting;

public sealed class FormatDocumentTests : AbstractLanguageServerProtocolTests
{
    public FormatDocumentTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestFormatDocumentAsync(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
            void M()
            {
                        int i = 1;{|caret:|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var documentURI = testLspServer.GetLocations("caret").Single().DocumentUri;
        await AssertFormatDocumentAsync(testLspServer, documentURI, """
            class A
            {
                void M()
                {
                    int i = 1;
                }
            }
            """);
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
        var options = new InitializationOptions
        {
            OptionUpdater = globalOptions =>
            {
                globalOptions.SetGlobalOption(LspOptionsStorage.LspOrganizeImportsOnFormat, LanguageNames.CSharp, true);
            },
        };

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, options);
        var documentURI = testLspServer.GetLocations("caret").Single().DocumentUri;
        await AssertFormatDocumentAsync(testLspServer, documentURI, """
            using System;
            using System.Collections;

            class A
            {
            }
            """);
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
        var options = new InitializationOptions
        {
            OptionUpdater = globalOptions =>
            {
                globalOptions.SetGlobalOption(LspOptionsStorage.LspOrganizeImportsOnFormat, LanguageNames.CSharp, true);
            },
        };

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, options);
        var documentURI = testLspServer.GetLocations("caret").Single().DocumentUri;
        await AssertFormatDocumentAsync(testLspServer, documentURI, """
            using System;
            using System.Collections;

            class A
            {
            }
            """);
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
        var options = new InitializationOptions
        {
            OptionUpdater = globalOptions =>
            {
                globalOptions.SetGlobalOption(LspOptionsStorage.LspOrganizeImportsOnFormat, LanguageNames.CSharp, true);
            },
        };

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, options);
        var documentURI = testLspServer.GetLocations("caret").Single().DocumentUri;
        await AssertFormatDocumentAsync(testLspServer, documentURI, """
            using System;
            using System.Collections;

            class A
            {
            }
            """);
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
        var options = new InitializationOptions
        {
            OptionUpdater = globalOptions =>
            {
                globalOptions.SetGlobalOption(LspOptionsStorage.LspOrganizeImportsOnFormat, LanguageNames.CSharp, true);
            },
        };

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, options);
        var documentURI = testLspServer.GetLocations("caret").Single().DocumentUri;
        await AssertFormatDocumentAsync(testLspServer, documentURI, """
            using System;
            using System.Collections;

            class A
            {
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task TestFormatDocumentWithOrganizeImports5Async(bool mutatingLspWorkspace)
    {
        var markup =
            """
            using   System.Collections;
            using  System;

            class A{|caret:|}
            {
            void M()
            {
                        int i = 1;
                }
            }
            """;
        var options = new InitializationOptions
        {
            OptionUpdater = globalOptions =>
            {
                globalOptions.SetGlobalOption(LspOptionsStorage.LspOrganizeImportsOnFormat, LanguageNames.CSharp, true);
            },
        };

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, options);
        var documentURI = testLspServer.GetLocations("caret").Single().DocumentUri;
        await AssertFormatDocumentAsync(testLspServer, documentURI, """
            using System;
            using System.Collections;

            class A
            {
                void M()
                {
                    int i = 1;
                }
            }
            """);
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
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var documentURI = testLspServer.GetLocations("caret").Single().DocumentUri;
        await AssertFormatDocumentAsync(testLspServer, documentURI, """
            using System.Collections;
            using System;

            class A
            {
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task TestFormatDocument_UseTabsAsync(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
            void M()
            {
            			int i = 1;{|caret:|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var documentURI = testLspServer.GetLocations("caret").Single().DocumentUri;
        await AssertFormatDocumentAsync(testLspServer, documentURI, """
            class A
            {
            	void M()
            	{
            		int i = 1;
            	}
            }
            """, insertSpaces: false, tabSize: 4);
    }

    [Theory, CombinatorialData]
    public async Task TestFormatDocument_ModifyTabIndentSizeAsync(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
            void M()
            {
            			int i = 1;{|caret:|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var documentURI = testLspServer.GetLocations("caret").Single().DocumentUri;
        await AssertFormatDocumentAsync(testLspServer, documentURI, """
            class A
            {
              void M()
              {
                int i = 1;
              }
            }
            """, insertSpaces: true, tabSize: 2);
    }

    private static async Task AssertFormatDocumentAsync(
        TestLspServer testLspServer,
        DocumentUri uri,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedText,
        bool insertSpaces = true,
        int tabSize = 4
        )
    {
        var documentText = await testLspServer.GetDocumentTextAsync(uri);

        var results = await RunFormatDocumentAsync(testLspServer, uri, insertSpaces, tabSize);
        var actualText = ApplyTextEdits(results, documentText);
        AssertEx.EqualOrDiff(expectedText, actualText);
    }

    private static async Task<LSP.TextEdit[]?> RunFormatDocumentAsync(
        TestLspServer testLspServer,
        DocumentUri uri,
        bool insertSpaces = true,
        int tabSize = 4)
    {
        return await testLspServer.ExecuteRequestAsync<LSP.DocumentFormattingParams, LSP.TextEdit[]>(LSP.Methods.TextDocumentFormattingName,
            CreateDocumentFormattingParams(uri, insertSpaces, tabSize), CancellationToken.None);
    }

    private static LSP.DocumentFormattingParams CreateDocumentFormattingParams(DocumentUri uri, bool insertSpaces, int tabSize)
        => new()
        {
            TextDocument = CreateTextDocumentIdentifier(uri),
            Options = new LSP.FormattingOptions()
            {
                InsertSpaces = insertSpaces,
                TabSize = tabSize,
            }
        };
}
