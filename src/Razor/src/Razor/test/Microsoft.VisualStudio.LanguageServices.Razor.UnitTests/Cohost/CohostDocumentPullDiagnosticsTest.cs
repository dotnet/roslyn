// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Remote.Razor.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public partial class CohostDocumentPullDiagnosticsTest
{
    [Fact]
    public async Task CSharpUnusedUsings_WarningDiagnosticsInVS()
    {
        var document = CreateProjectAndRazorDocument("""
            @using System
            @using System.Text

            <div></div>

            @code
            {
                public void BuildsStrings(StringBuilder b)
                {
                }
            }
            """);

        var requestInvoker = new TestHtmlRequestInvoker();
        var result = await MakeDiagnosticsRequestAsync(document, taskListRequest: false, requestInvoker, IncompatibleProjectService, RemoteServiceInvoker, ClientCapabilitiesService, LoggerFactory, DisposalToken);

        Assert.NotNull(result);
        var diagnostic = Assert.Single(result);
        Assert.Equal(0, diagnostic.Range.Start.Line);
        Assert.Equal(0, diagnostic.Range.End.Line);
        Assert.Equal("RZ0005", diagnostic.Code.AssumeNotNull().Second);
        Assert.Equal(LspDiagnosticSeverity.Warning, diagnostic.Severity);

        var tags = Assert.IsType<DiagnosticTag[]>(diagnostic.Tags);
        Assert.Collection(
            tags,
            tag => Assert.Equal(VSDiagnosticTags.HiddenInEditor, tag),
            tag => Assert.Equal(DiagnosticTag.Unnecessary, tag));
    }

    [Fact]
    public async Task DiagnosticMetadata_InVS()
    {
        var result = await VerifyDiagnosticsAsync("""
            @{|CS0103:CallMeMaybe|}()
            """);

        var diagnostic = Assert.IsType<VSDiagnostic>(Assert.Single(result));
        Assert.NotNull(diagnostic.Identifier);
        Assert.NotNull(diagnostic.Projects);
        Assert.NotNull(Assert.Single(diagnostic.Projects).ProjectIdentifier);
    }

    [Fact]
    public Task OneOfEachDiagnostic()
    {
        TestCode input = """
            <div>

            {|HTM1337:<not_a_tag />|}

            {|RZ10012:<NonExistentComponent />|}

            </div>

            <script>
                {|TS2304:let foo: string = 42;|}
            </script>

            <style>
                {|CSS002:f|}oo
                {
                    bar: baz;
                }
            </style>

            @code
            {
                public void IJustMetYou()
                {
                    {|CS0103:CallMeMaybe|}();
                }
            }
            """;

        return VerifyDiagnosticsAsync(input,
           htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new VSDiagnostic
                    {
                        Code = "HTM1337",
                        Range = SourceText.From(input.Text).GetRange(input.NamedSpans["HTM1337"].First()),
                        Projects = [new VSDiagnosticProjectInformation()
                        {
                            ProjectIdentifier = "Html"
                        }]
                    },
                    new VSDiagnostic
                    {
                        Code = "TS2304",
                        Range = SourceText.From(input.Text).GetRange(input.NamedSpans["TS2304"].First()),
                        Projects = [new VSDiagnosticProjectInformation()
                        {
                            ProjectIdentifier = "TypeScript"
                        }]
                    },
                    new VSDiagnostic
                    {
                        Code = "CSS002",
                        Range = SourceText.From(input.Text).GetRange(input.NamedSpans["CSS002"].First()),
                        Projects = [new VSDiagnosticProjectInformation()
                        {
                            ProjectIdentifier = "CSS"
                        }]
                    },
                ]
            }]);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13251")]
    public Task FilterTypeScriptExpressionExpectedAfterRazorExpression()
    {
        TestCode input = """
            <script>
                const values = @Html.Raw("[]");
                const next = 0;
            </script>
            """;

        return VerifyDiagnosticsAsync(
            input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = "TS1109",
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf(';'), 1))
                    }
                ]
            }],
            fileKind: RazorFileKind.Legacy);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13251")]
    public Task DoNotFilterTypeScriptExpressionExpectedInJavaScript()
    {
        TestCode input = """
            <script>
                const values = {|TS1109:;|}
            </script>
            """;

        return VerifyDiagnosticsAsync(
            input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = "TS1109",
                        Range = SourceText.From(input.Text).GetRange(input.NamedSpans["TS1109"].First())
                    }
                ]
            }],
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public Task Html()
    {
        TestCode input = """
            <div>

            {|HTM1337:<not_a_tag />|}

            </div>
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = "HTM1337",
                        Range = SourceText.From(input.Text).GetRange(input.NamedSpans.First().Value.First())
                    }
                ]
            }]);
    }

    [Fact]
    public Task FilterEscapedAtFromCss()
    {
        TestCode input = """
            <div>

            <style>
              @@media (max-width: 600px) {
                body {
                  background-color: lightblue;
                }
              }

              {|CSS002:f|}oo
              {
                bar: baz;
              }
            </style>

            </div>
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.UnrecognizedBlockType,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("@@") + 1, 1))
                    },
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.UnrecognizedBlockType,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("f"), 1))
                    }
                ]
            }]);
    }

    [Fact]
    public Task FilterCSharpFromCss()
    {
        TestCode input = """
            <div>

            <style>
                @{ insertSomeBigBlobOfCSharp(); }

                {|CSS031:~|}~~~~
            </style>

            </div>

            @code {
                string insertSomeBigBlobOfCSharp() => "body { font-weight: bold; }";
            }
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingSelectorBeforeCombinatorCode,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("@{"), 1))
                    },
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingSelectorBeforeCombinatorCode,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("~~"), 1))
                    }
                ]
            }]);
    }

    [Fact]
    public Task FilterRazorCommentsFromCss()
    {
        TestCode input = """
            <div>

            <style>
                @* This is a Razor comment *@

                {|CSS031:~|}~~~~
            </style>

            </div>
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingSelectorBeforeCombinatorCode,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("@*"), 1))
                    },
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingSelectorBeforeCombinatorCode,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("~~"), 1))
                    }
                ]
            }]);
    }

    [Fact]
    public Task FilterRazorCommentsFromCss_Inside()
    {
        TestCode input = """
            <div>

            <style>
                @* This is a Razor comment *@

                {|CSS031:~|}~~~~
            </style>

            </div>
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingSelectorBeforeCombinatorCode,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("Ra"), 1))
                    },
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingSelectorBeforeCombinatorCode,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("~~"), 1))
                    }
                ]
            }]);
    }

    [Fact]
    public Task FilterMissingClassNameInCss()
    {
        TestCode input = """
            <div>

            <style>
              .@(className)
                background-color: lightblue;
              }

              .{|CSS008:{|}
                bar: baz;
              }
            </style>

            </div>

            @code
            {
                private string className = "foo";
            }
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingClassNameAfterDot,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf(".@") + 1, 1))
                    },
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingClassNameAfterDot,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf(".{") + 1, 1))
                    },
                ]
            }]);
    }

    [Fact]
    public Task FilterMissingClassNameInCss_WithSpace()
    {
        TestCode input = """
            <div>

            <style>
              . @(className)
                background-color: lightblue;
              }

              .{|CSS008: |}{
                bar: baz;
              }
            </style>

            </div>

            @code
            {
                private string className = "foo";
            }
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingClassNameAfterDot,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf(". @") + 1, 1))
                    },
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingClassNameAfterDot,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf(". {") + 1, 1))
                    },
                ]
            }]);
    }

    [Fact]
    public Task FilterPropertyValueInCss()
    {
        TestCode input = """
            <div>

            <style>
              .goo {
                background-color: @(color);
              }

              .foo {
                background-color:{|CSS025: |}/* no value here */;
              }
            </style>

            </div>

            @code
            {
                private string color = "red";
            }
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingPropertyValue,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf(": @") + 1, 1))
                    },
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingPropertyValue,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf(": /") + 1, 1))
                    },
                ]
            }]);
    }

    [Fact]
    public Task FilterPropertyNameInCss()
    {
        const string CSharpExpression = """@(someBool ? "width: 100%" : "width: 50%")""";
        TestCode input = $$"""
            <div style="{|CSS024:/****/|}"></div>
            <div style="{{CSharpExpression}}">

            </div>

            @code
            {
                private bool someBool = false;
            }
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingPropertyName,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("/"), "/****/".Length))
                    },
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingPropertyName,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("@"), CSharpExpression.Length))
                    },
                ]
            }]);
    }

    [Fact]
    public Task DontFilterPropertyNameInStyleBlock()
    {
        TestCode input = """
            <style>
                .foo {
                    {|CSS024:/****/|}
                }
            </style>
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingPropertyName,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("/"), "/****/".Length))
                    },
                ]
            }]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/13191")]
    public Task FilterPropertyNameOutsideAttribute()
    {
        TestCode input = """
            @if (true)
            {
                <MudCard Outlined="true" Class="mud-width-full" Elevation="3"
                         HeaderClass="@((SelectedFileId == DefaultMessageId) ? "selected" : "unselected")">
                    <div class="d-flex flex-column align-center justify-center pa-3" style="height: 120px;">
                        <span style="width: 179px; height: 100%; background-color: black;"></span>
                    </div>

                    <div class="d-flex card-actions-cursor" style="padding-bottom: 10px; padding-top: 10px; border-top: 1px solid var(--mud-palette-lines-default);">
                        <span>Default message</span>
                    </div>
                </MudCard>
            }

            <div style="{|CSS024:/****/|}"></div>

            @code
            {
                private string SelectedFileId { get; set; } = "";
                private string DefaultMessageId { get; } = "Default";
            }
            """;

        var sourceText = SourceText.From(input.Text);

        // Sometimes the Html server will report the diagnostic range as the whole component, or even multiple.
        var diagnosticStart = input.Text.IndexOf("    <MudCard");
        var diagnosticEnd = input.Text.IndexOf("        <div class=\"d-flex card-actions-cursor\"");

        return VerifyDiagnosticsAsync(
            input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingPropertyName,
                        Range = sourceText.GetRange(TextSpan.FromBounds(diagnosticStart, diagnosticEnd))
                    },
                    new LspDiagnostic
                    {
                        Code = CSSErrorCodes.MissingPropertyName,
                        Range = sourceText.GetRange(new TextSpan(input.Text.IndexOf("/****/"), "/****/".Length))
                    },
                ]
            }],
            additionalFiles:
            [
                (FilePath("MudCard.razor"), """
                    @code
                    {
                        [Parameter]
                        public bool Outlined { get; set; }

                        [Parameter]
                        public string Class { get; set; }

                        [Parameter]
                        public int Elevation { get; set; }

                        [Parameter]
                        public string HeaderClass { get; set; }

                        [Parameter]
                        public RenderFragment ChildContent { get; set; }
                    }
                    """)
            ]);
    }

    [Fact]
    public Task FilterFromMultilineComponentAttributes()
    {
        var firstLine = "Hello this is a";
        TestCode input = $$"""
            <File1 Title="{{firstLine}}
                          multiline attribute" />

            @code
            {
                [Parameter]
                public string Title { get; set; }
            }
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = HtmlErrorCodes.MismatchedAttributeQuotesErrorCode,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf(firstLine), firstLine.Length))
                    },
                ]
            }]);
    }

    [Fact]
    public Task DontFilterFromMultilineHtmlAttributes()
    {
        var firstLine = "Hello this is a";
        TestCode input = $$"""
            <div class="{|HTML0005:{{firstLine}}|}
                        multiline attribute" />
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = HtmlErrorCodes.MismatchedAttributeQuotesErrorCode,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf(firstLine), firstLine.Length))
                    },
                ]
            }]);
    }

    [Theory]
    [InlineData("", "\"")]
    [InlineData("", "'")]
    [InlineData("@onclick=\"Send\"", "\"")] // The @onclick makes the disabled attribute a TagHelperAttributeSyntax
    [InlineData("@onclick='Send'", "'")]
    public Task FilterBadAttributeValueInHtml(string extraTagContent, string quoteChar)
    {
        TestCode input = $$"""
            <button {{extraTagContent}} disabled={{quoteChar}}@(!EnableMyButton){{quoteChar}}>Send</button>
            <button disabled={{quoteChar}}{|HTML0209:ThisIsNotValid|}{{quoteChar}} />

            @code
            {
                private bool EnableMyButton => true;

                Task Send() =>
                    Task.CompletedTask;
            }
            """;

        return VerifyDiagnosticsAsync(input,
            htmlResponse: [new FullDocumentDiagnosticReport
            {
                Items =
                [
                    new LspDiagnostic
                    {
                        Code = HtmlErrorCodes.UnknownAttributeValueErrorCode,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("@("), "@(!EnableMyButton)".Length))
                    },
                    new LspDiagnostic
                    {
                        Code = HtmlErrorCodes.UnknownAttributeValueErrorCode,
                        Range = SourceText.From(input.Text).GetRange(new TextSpan(input.Text.IndexOf("T"), "ThisIsNotValid".Length))
                    },
                ]
            }]);
    }

    [Fact]
    public Task TODOComments()
        => VerifyDiagnosticsAsync("""
            @using System.Threading.Tasks;

            // TODO: This isn't C#

            @{
                // {|TODO:|}TODO: This is C# in an impl document
            }

            TODO: Nor is this

            <div>

                @*{|TODO: TODO: This does |}*@

                @* TODONT: This doesn't *@

            </div>

            @code {
                // This looks different because Roslyn only reports zero width ranges for task lists
                // {|TODO:|}TODO: Write some C# code in a decl document too
            }
            """,
            taskListRequest: true);

    [Fact]
    public Task TODOComments_NoDecl()
        => VerifyDiagnosticsAsync("""
            @using System.Threading.Tasks;

            @{
                // {|TODO:|}TODO: This is C# in an impl document
            }

            TODO: Nor is this

            <div>

                @*{|TODO: TODO: This does |}*@

                @* TODONT: This doesn't *@

            </div>
            """,
            taskListRequest: true);
}
