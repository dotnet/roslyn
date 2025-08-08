// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.EmbeddedLanguages;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EmbeddedLanguages;

[Trait(Traits.Feature, Traits.Features.ValidateJsonString)]
public sealed class ValidateJsonStringTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public ValidateJsonStringTests(ITestOutputHelper logger) : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider?) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpJsonDiagnosticAnalyzer(), null);

    private OptionsCollection OptionOn()
        => Option(JsonDetectionOptionsStorage.ReportInvalidJsonPatterns, true);

    [Fact]
    public Task TestWarning1()
        => TestDiagnosticInfoAsync("""
            class Program
            {
                void Main()
                {
                    var r = /*lang=json,strict*/ "[|new|] Json()";
                }     
            }
            """,
            options: OptionOn(),
            diagnosticId: AbstractJsonDiagnosticAnalyzer.DiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Warning,
            diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0, FeaturesResources.Constructors_not_allowed));

    [Fact]
    public Task TestWarningInRawString1()
        => TestDiagnosticInfoAsync(""""
            class Program
            {
                void Main()
                {
                    var r = /*lang=json,strict*/ """[|new|] Json()""";
                }     
            }
            """",
            options: OptionOn(),
            diagnosticId: AbstractJsonDiagnosticAnalyzer.DiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Warning,
            diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0, FeaturesResources.Constructors_not_allowed));

    [Fact]
    public Task TestWarning2()
        => TestDiagnosticInfoAsync("""
            class Program
            {
                void Main()
                {
                    var r = /*lang=json*/ "[|}|]";
                }     
            }
            """,
            options: OptionOn(),
            diagnosticId: AbstractJsonDiagnosticAnalyzer.DiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Warning,
            diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0,
                string.Format(FeaturesResources._0_unexpected, '}')));

    [Fact]
    public Task TestJsonDocumentWithTrailingComma()
        => TestDiagnosticInfoAsync("""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    var r = JsonDocument.Parse(@"[1[|,|]]");
                }     
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            options: OptionOn(),
            diagnosticId: AbstractJsonDiagnosticAnalyzer.DiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Warning,
            diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0,
                FeaturesResources.Trailing_comma_not_allowed));

    [Fact]
    public Task TestJsonDocumentTrailingCommaDisallowed()
        => TestDiagnosticInfoAsync("""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    var r = JsonDocument.Parse(@"[1[|,|]]", new JsonDocumentOptions { AllowTrailingCommas = false });
                }     
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            options: OptionOn(),
            diagnosticId: AbstractJsonDiagnosticAnalyzer.DiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Warning,
            diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0,
                FeaturesResources.Trailing_comma_not_allowed));

    [Fact]
    public Task TestJsonDocumentTrailingCommaAllowed()
        => TestDiagnosticMissingAsync("""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    var r = JsonDocument.Parse(@"[1[|,|]]", new JsonDocumentOptions { AllowTrailingCommas = true });
                }     
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestJsonDocumentTrailingCommaAllowedImplicitObject()
        => TestDiagnosticMissingAsync("""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    var r = JsonDocument.Parse(@"[1[|,|]]", new() { AllowTrailingCommas = true });
                }     
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestJsonDocumentWithComments()
        => TestDiagnosticInfoAsync("""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    var r = JsonDocument.Parse(@"[1][|/*comment*/|]");
                }     
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            options: OptionOn(),
            diagnosticId: AbstractJsonDiagnosticAnalyzer.DiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Warning,
            diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0,
                FeaturesResources.Comments_not_allowed));

    [Fact]
    public Task TestJsonDocumentCommentsDisallowed()
        => TestDiagnosticInfoAsync("""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    var r = JsonDocument.Parse(@"[1][|/*comment*/|]", new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Disallow });
                }     
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            options: OptionOn(),
            diagnosticId: AbstractJsonDiagnosticAnalyzer.DiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Warning,
            diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0,
                FeaturesResources.Comments_not_allowed));

    [Fact]
    public Task TestJsonDocumentCommentsAllowed()
        => TestDiagnosticMissingAsync("""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    var r = JsonDocument.Parse(@"[1][|/*comment*/|]", new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Allow });
                }     
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestJsonDocumentCommentsAllowedImplicitObject()
        => TestDiagnosticMissingAsync("""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    var r = JsonDocument.Parse(@"[1][|/*comment*/|]", new() { CommentHandling = JsonCommentHandling.Allow });
                }     
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestJsonDocumentCommentsDisallowed_StringSyntaxAttribute_NoOptionsProvided()
        => TestDiagnosticInfoAsync($$"""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Diagnostics.CodeAnalysis;
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    M(@"[1][|/*comment*/|]");
                }

                void M([StringSyntax(StringSyntaxAttribute.Json)] string p)
                {
                }
            }
            {{EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharpXml}}
                    </Document>
                </Project>
            </Workspace>
            """,
            options: OptionOn(),
            diagnosticId: AbstractJsonDiagnosticAnalyzer.DiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Warning,
            diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0,
                FeaturesResources.Comments_not_allowed));

    [Fact]
    public Task TestJsonDocumentCommentsDisallowed_StringSyntaxAttribute_OptionsProvided()
        => TestDiagnosticInfoAsync($$"""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Diagnostics.CodeAnalysis;
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    M(@"[1][|/*comment*/|]", new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Disallow });
                }

                void M([StringSyntax(StringSyntaxAttribute.Json)] string p, JsonDocumentOptions options)
                {
                }
            }
            {{EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharpXml}}
                    </Document>
                </Project>
            </Workspace>
            """,
            options: OptionOn(),
            diagnosticId: AbstractJsonDiagnosticAnalyzer.DiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Warning,
            diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0,
                FeaturesResources.Comments_not_allowed));

    [Fact]
    public Task TestJsonDocumentCommentsAllowed_StringSyntaxAttribute_OptionsProvided()
        => TestDiagnosticMissingAsync($$"""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Diagnostics.CodeAnalysis;
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    M(@"[1][|/*comment*/|]", new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Allow });
                }

                void M([StringSyntax(StringSyntaxAttribute.Json)] string p, JsonDocumentOptions options)
                {
                }
            }
            {{EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeCSharpXml}}
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestNotOnUnlikelyJson()
        => TestDiagnosticMissingAsync($$"""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Diagnostics.CodeAnalysis;
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    var v = [|"[1, 2, 3]"|];
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestNotOnLikelyJson()
        => TestDiagnosticMissingAsync($$"""
            <Workspace>
                <Project Language="C#" CommonReferencesNet6="true">
                    <Document>
            using System.Diagnostics.CodeAnalysis;
            using System.Text.Json;

            class Program
            {
                void Main()
                {
                    var v = [|"{ prop: 0 }"|];
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);
}
