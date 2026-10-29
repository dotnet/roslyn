// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostUriPresentationEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task RandomFile()
    {
        await VerifyUriPresentationAsync(
            input: """
                This is a Razor document.

                <div>
                    [||]
                </div>

                The end.
                """,
            uris: [FileUri("SomeRandomFile.txt")],
            // In reality this would actual insert the full path, but the Html server does that for us, and we
            // have other tests that validate that we insert what the Html server tells us
            expected: null);
    }

    [Fact]
    public async Task HtmlResponse_TranslatesVirtualDocumentUri()
    {
        var siteCssFileUriString = "file:///C:/path/to/site.css";
        var htmlTag = $"""<link src="{siteCssFileUriString}" rel="stylesheet" />""";

        await VerifyUriPresentationAsync(
            input: """
                This is a Razor document.

                <div>
                    [||]
                </div>

                The end.
                """,
            uris: [new(siteCssFileUriString)],
            htmlResponse: new WorkspaceEdit
            {
                DocumentChanges = new TextDocumentEdit[]
                {
                    new()
                    {
                        TextDocument = new()
                        {
                            DocumentUri = FileUri($"File1.razor{LanguageServerConstants.HtmlVirtualDocumentSuffix}")
                        },
                        Edits = [LspFactory.CreateTextEdit(position: (0, 0), htmlTag)]
                    }
                }
            },
            expected: htmlTag);
    }

    [Fact]
    public async Task Component()
    {
        await VerifyUriPresentationAsync(
            input: """
                This is a Razor document.

                <div>
                    [||]
                </div>

                The end.
                """,
            additionalFiles: [
                (FilePath("Component.razor"), "")
            ],
            uris: [FileUri("Component.razor")],
            expected: "<Component />");
    }

    [Fact]
    public async Task Component_DifferentNamespace_NoUsing()
    {
        await VerifyUriPresentationAsync(
            input: """
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [
                (FilePath("Other\\Component.razor"), "")
            ],
            uris: [FileUri("Other\\Component.razor")],
            expected: "<SomeProject.Other.Component />");
    }

    [Fact]
    public async Task Component_DifferentNamespace_NoUsing_ReplacesSelection()
    {
        await VerifyUriPresentationAsync(
            input: """
                <div>
                    [|replace me|]
                </div>
                """,
            additionalFiles: [
                (FilePath("Other\\Component.razor"), "")
            ],
            uris: [FileUri("Other\\Component.razor")],
            expected: "<SomeProject.Other.Component />");
    }

    [Fact]
    public async Task Component_SameNamespace()
    {
        await VerifyUriPresentationAsync(
            input: """
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [(FilePath("Nested\\Component.razor"), "")],
            uris: [FileUri("Nested\\Component.razor")],
            expected: "<Component />",
            documentFilePath: FilePath("Nested\\File1.razor"));
    }

    [Fact]
    public async Task Component_AncestorNamespace()
    {
        await VerifyUriPresentationAsync(
            input: """
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [(FilePath("Component.razor"), "")],
            uris: [FileUri("Component.razor")],
            expected: "<Component />",
            documentFilePath: FilePath("Nested\\File1.razor"));
    }

    [Fact]
    public async Task Component_ExplicitNamespace_InScope()
    {
        await VerifyUriPresentationAsync(
            input: """
                @namespace Custom.Components
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [(FilePath("Other\\Component.razor"), "@namespace Custom.Components")],
            uris: [FileUri("Other\\Component.razor")],
            expected: "<Component />");
    }

    [Fact]
    public async Task Component_ExplicitNamespace_OutOfScope()
    {
        await VerifyUriPresentationAsync(
            input: """
                @namespace Destination.Components
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [(FilePath("Other\\Component.razor"), "@namespace Custom.Components")],
            uris: [FileUri("Other\\Component.razor")],
            expected: "<Custom.Components.Component />");
    }

    [Fact]
    public async Task Component_Using()
    {
        await VerifyUriPresentationAsync(
            input: """
                @using SomeProject.Other
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [(FilePath("Other\\Component.razor"), "")],
            uris: [FileUri("Other\\Component.razor")],
            expected: "<Component />");
    }

    [Fact]
    public async Task Component_Using_GlobalPrefix()
    {
        await VerifyUriPresentationAsync(
            input: """
                @using global::SomeProject.Other
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [(FilePath("Other\\Component.razor"), "")],
            uris: [FileUri("Other\\Component.razor")],
            expected: "<Component />");
    }

    [Fact]
    public async Task Component_UsingAlias_DoesNotBringComponentIntoScope()
    {
        await VerifyUriPresentationAsync(
            input: """
                @using Alias = SomeProject.Other
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [(FilePath("Other\\Component.razor"), "")],
            uris: [FileUri("Other\\Component.razor")],
            expected: "<SomeProject.Other.Component />");
    }

    [Fact]
    public async Task Component_StaticUsing_DoesNotBringComponentIntoScope()
    {
        await VerifyUriPresentationAsync(
            input: """
                @using static SomeProject.Other.Component
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [(FilePath("Other\\Component.razor"), "")],
            uris: [FileUri("Other\\Component.razor")],
            expected: "<SomeProject.Other.Component />");
    }

    [Fact]
    public async Task Component_Imports_Inherited()
    {
        await VerifyUriPresentationAsync(
            input: """
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [
                (FilePath("Other\\Component.razor"), ""),
                (FilePath("Pages\\_Imports.razor"), "@using SomeProject.Other")
            ],
            uris: [FileUri("Other\\Component.razor")],
            expected: "<Component />",
            documentFilePath: FilePath("Pages\\File1.razor"));
    }

    [Fact]
    public async Task Component_Imports_NotInherited()
    {
        await VerifyUriPresentationAsync(
            input: """
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [
                (FilePath("Other\\Component.razor"), ""),
                (FilePath("Elsewhere\\_Imports.razor"), "@using SomeProject.Other")
            ],
            uris: [FileUri("Other\\Component.razor")],
            expected: "<SomeProject.Other.Component />",
            documentFilePath: FilePath("Pages\\File1.razor"));
    }

    [Fact]
    public async Task Component_AmbiguousShortName()
    {
        await VerifyUriPresentationAsync(
            input: """
                @using SomeProject.Other
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [
                (FilePath("Component.razor"), ""),
                (FilePath("Other\\Component.razor"), "")
            ],
            uris: [FileUri("Other\\Component.razor")],
            expected: "<SomeProject.Other.Component />");
    }

    [Fact]
    public async Task Component_ShortNameResolvesToDifferentComponent()
    {
        await VerifyUriPresentationAsync(
            input: """
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [
                (FilePath("Component.razor"), ""),
                (FilePath("Other\\Component.razor"), "")
            ],
            uris: [FileUri("Other\\Component.razor")],
            expected: "<SomeProject.Other.Component />");
    }

    [Fact]
    public async Task Component_Generic_InScope()
    {
        await VerifyUriPresentationAsync(
            input: """
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [
                (FilePath("Component.razor"),
                    """
                    @typeparam TItem
                    @code
                    {
                        [Parameter]
                        [EditorRequired]
                        public TItem Value { get; set; }
                    }
                    """)
            ],
            uris: [FileUri("Component.razor")],
            expected: """<Component Value="" />""");
    }

    [Fact]
    public async Task Component_Generic_OutOfScope()
    {
        await VerifyUriPresentationAsync(
            input: """
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [
                (FilePath("Other\\Component.razor"),
                    """
                    @typeparam TItem
                    @code
                    {
                        [Parameter]
                        [EditorRequired]
                        public TItem Value { get; set; }
                    }
                    """)
            ],
            uris: [FileUri("Other\\Component.razor")],
            expected: """<SomeProject.Other.Component Value="" />""");
    }

    [Fact]
    public async Task Component_IntoLegacy_DelegatesToHtml()
    {
        var htmlResponseText = "HTML response";

        await VerifyUriPresentationAsync(
            input: """
                <div>
                    [||]
                </div>
                """,
            additionalFiles: [(FilePath("Other\\Component.razor"), "")],
            uris: [FileUri("Other\\Component.razor")],
            htmlResponse: new WorkspaceEdit
            {
                DocumentChanges = new TextDocumentEdit[]
                {
                    new()
                    {
                        TextDocument = new()
                        {
                            DocumentUri = FileUri($"File1.cshtml{LanguageServerConstants.HtmlVirtualDocumentSuffix}")
                        },
                        Edits = [LspFactory.CreateTextEdit(position: (0, 0), htmlResponseText)]
                    }
                }
            },
            expected: htmlResponseText,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task ImportsFile()
    {
        await VerifyUriPresentationAsync(
            input: """
                This is a Razor document.

                <div>
                    [||]
                </div>

                The end.
                """,
            uris: [FileUri("_Imports.razor")],
            expected: null);
    }

    [Fact]
    public async Task Html_IntoCSharp_NoTag()
    {
        var siteCssFileUriString = "file:///C:/path/to/site.css";
        var htmlTag = $"""<link src="{siteCssFileUriString}" rel="stylesheet" />""";

        await VerifyUriPresentationAsync(
            input: """
                This is a Razor document.

                <div>
                </div>

                @code {
                    [||]
                }
                """,
            uris: [new(siteCssFileUriString)],
            htmlResponse: new WorkspaceEdit
            {
                DocumentChanges = new TextDocumentEdit[]
                {
                    new()
                    {
                        TextDocument = new()
                        {
                            DocumentUri = FileUri("File1.razor.g.html")
                        },
                        Edits = [LspFactory.CreateTextEdit(position: (0, 0), htmlTag)]
                    }
                }
            },
            expected: null);
    }

    [Fact]
    public async Task Component_IntoCSharp_NoTag()
    {
        await VerifyUriPresentationAsync(
            input: """
                This is a Razor document.

                <div>
                </div>

                @code {
                    [||]
                }
                """,
            additionalFiles: [
                (FilePath("Component.razor"), "")
            ],
            uris: [FileUri("Component.razor")],
            expected: null);
    }

    [Fact]
    public async Task Component_WithChildFile()
    {
        await VerifyUriPresentationAsync(
            input: """
                This is a Razor document.

                <div>
                    [||]
                </div>

                The end.
                """,
            additionalFiles: [
                (FilePath("Component.razor"), "")
            ],
            uris: [
                FileUri("Component.razor"),
                FileUri("Component.razor.css"),
                FileUri("Component.razor.js")
            ],
            expected: "<Component />");
    }

    [Fact]
    public async Task Component_WithChildFile_RazorNotFirst()
    {
        await VerifyUriPresentationAsync(
            input: """
                This is a Razor document.

                <div>
                    [||]
                </div>

                The end.
                """,
            additionalFiles: [
                (FilePath("Component.razor"), "")
            ],
            uris: [
                FileUri("Component.razor.css"),
                FileUri("Component.razor"),
                FileUri("Component.razor.js")
            ],
            expected: "<Component />");
    }

    [Fact]
    public async Task Component_RequiredParameter()
    {
        await VerifyUriPresentationAsync(
            input: """
                This is a Razor document.

                <div>
                    [||]
                </div>

                The end.
                """,
            additionalFiles: [
                (FilePath("Component.razor"),
                    """
                    @code
                    {
                        [Parameter]
                        [EditorRequired]
                        public string RequiredParameter { get; set; }

                        [Parameter]
                        public string NormalParameter { get; set; }
                    }
                    """)
            ],
            uris: [FileUri("Component.razor")],
            expected: """<Component RequiredParameter="" />""");
    }

    [Fact]
    public async Task Component_RequiredParameter_OutOfScope()
    {
        await VerifyUriPresentationAsync(
            input: """
                This is a Razor document.

                <div>
                    [||]
                </div>

                The end.
                """,
            additionalFiles: [
                (FilePath("Other\\Component.razor"),
                    """
                    @code
                    {
                        [Parameter]
                        [EditorRequired]
                        public string RequiredParameter { get; set; }

                        [Parameter]
                        public string NormalParameter { get; set; }
                    }
                    """)
            ],
            uris: [FileUri("Other\\Component.razor")],
            expected: """<SomeProject.Other.Component RequiredParameter="" />""");
    }

    private async Task VerifyUriPresentationAsync(string input, DocumentUri[] uris, string? expected, WorkspaceEdit? htmlResponse = null, (string fileName, string contents)[]? additionalFiles = null, RazorFileKind? fileKind = null, string? documentFilePath = null)
    {
        TestFileMarkupParser.GetSpan(input, out input, out var span);
        var document = CreateProjectAndRazorDocument(input, fileKind: fileKind, documentFilePath: documentFilePath, additionalFiles: additionalFiles);
        var sourceText = await document.GetTextAsync(DisposalToken);

        var requestInvoker = new TestHtmlRequestInvoker([(VSInternalMethods.TextDocumentUriPresentationName, htmlResponse)]);

        var endpoint = new CohostUriPresentationEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker);

        var request = new VSInternalUriPresentationParams()
        {
            TextDocument = new TextDocumentIdentifier()
            {
                DocumentUri = document.GetURI()
            },
            Range = sourceText.GetRange(span),
            Uris = [.. uris.Select(u => u.GetRequiredSystemUri())]
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        if (expected is null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
            Assert.NotNull(result.DocumentChanges);
            Assert.Equal(expected, ((TextEdit)result.DocumentChanges.Value.First[0].Edits[0]).NewText);
            Assert.Equal(document.GetURI(), result.DocumentChanges.Value.First[0].TextDocument.DocumentUri);
        }
    }
}
