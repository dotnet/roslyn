// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using Xunit;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

[WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
public class DocumentationDirectiveCodeGenerationTest()
    : RazorBaselineIntegrationTestBase(TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;
    internal override string DefaultFileName => "TestComponent.razor";

    protected override string GetDirectoryPath(string testName)
        => Path.Combine("TestFiles", "IntegrationTests", nameof(DocumentationDirectiveCodeGenerationTest), testName);

    [Fact]
    public void Documentation()
    {
        VerifyBaseline("""
            @attribute [System.Serializable]
            @documentation {
                <summary>
                Hello from <see cref="System.String"/>.
                </summary>
                <remarks>More documentation.</remarks>
            }
            <p>Rendered content</p>
            """);
    }

    [Fact]
    public void SingleLine()
    {
        var result = VerifyBaseline("""
            @documentation {<summary>Uses <see cref="System.DateTime"/>.</summary>}
            <p>After</p>
            """);

        Assert.Empty(result.RazorDiagnostics);
    }

    [Fact]
    public void XmlOnOpeningBraceLine()
    {
        var result = VerifyBaseline("""
            @documentation {<summary>
                Uses <see cref="System.DateTime"/>.
                </summary>
            }
            <p>After</p>
            """);

        Assert.Empty(result.RazorDiagnostics);
    }

    [Fact]
    public void XmlOnClosingBraceLine()
    {
        var result = VerifyBaseline("""
            @documentation {
                <summary>
                Uses <see cref="System.DateTime"/>.
                </summary>}
            <p>After</p>
            """);

        Assert.Empty(result.RazorDiagnostics);
    }

    [Fact]
    public void XmlOnBothBraceLines()
    {
        var result = VerifyBaseline("""
            @documentation {<summary>
                Uses <see cref="System.DateTime"/>.
                </summary>}
            <p>After</p>
            """);

        Assert.Empty(result.RazorDiagnostics);
    }

    [Fact]
    public void RawXml()
    {
        VerifyBaseline("""
            @documentation {
                <summary>It's raw XML with @transitions and }.</summary>
                <remarks><![CDATA[<p>{ content }</p>]]></remarks>
                <!-- @* Not a Razor comment *@ -->
            }
            <p>After</p>
            """);
    }

    [Fact]
    public void MalformedInlineXml()
    {
        var result = VerifyBaseline("""
            @documentation {<summary>Missing end tag}
            <p>After</p>
            @code { /* note */ public int Value => 42; }
            """);

        Assert.Empty(result.RazorDiagnostics);
    }

    [Fact]
    public void ClosingTagInFollowingCode()
    {
        var result = VerifyBaseline("""
            @documentation {<summary>Missing end tag}
            <p>After</p>
            @code { public string EndTag => "</summary>"; /* note */ }
            """);

        Assert.Empty(result.RazorDiagnostics);
    }

    [Fact]
    public void SameLineMarkupAfterMalformedXml()
    {
        var result = VerifyBaseline("""
            @documentation {<summary>Missing end tag}<p>After</p>
            @code { /* note */ public int Value => 42; }
            """);

        Assert.Empty(result.RazorDiagnostics);
    }

    [Fact]
    public void MissingClosingBraceBeforeCode()
    {
        var result = VerifyBaseline("""
            @documentation {<summary>Missing brace</summary>
            @code { /* note */ public int Value => 42; }
            <p>After</p>
            """);

        Assert.Equal("RZ1006", Assert.Single(result.RazorDiagnostics).Id);
    }

    [Fact]
    public void DuplicateDocumentation()
    {
        var result = VerifyBaseline("""
            @documentation {<summary>First</summary>}
            @documentation {<summary>Second</summary>}
            <p>After</p>
            """);

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal("RZ2001", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void CommentTerminator()
    {
        var result = VerifyBaseline("""
            @documentation {
                <summary>*/ [System.Obsolete] /*</summary>
            }
            <p>After</p>
            """);

        Assert.Equal("RZ1047", Assert.Single(result.RazorDiagnostics).Id);
    }

    [Fact]
    public void IdentifierInRazor11()
    {
        var result = VerifyBaseline("""
            @documentation foo
            @code {
                private string documentation => "Value";
            }
            """,
            configuration: Configuration with
            {
                LanguageVersion = RazorLanguageVersion.Version_11_0,
                RazorWarningLevel = 11
            });

        Assert.Equal("RZ1048", Assert.Single(result.RazorDiagnostics).Id);
    }

    [Fact]
    public void MissingOpeningBrace()
    {
        var result = VerifyBaseline("""
            @documentation foo
            <p>After</p>
            """);

        Assert.Equal("RZ1017", Assert.Single(result.RazorDiagnostics).Id);
    }

    private CompileToCSharpResult VerifyBaseline(
        string source,
        RazorConfiguration? configuration = null,
        [CallerMemberName] string testName = "")
    {
        var result = CompileToCSharp(source, configuration: configuration);
        AssertSyntaxTreeMatchesBaseline(result.CodeDocument, testName);
        AssertDocumentNodeMatchesBaseline(result.CodeDocument, testName);
        AssertCSharpDocumentMatchesBaseline(result.CodeDocument, testName: testName);
        CompileToAssembly(result);
        return result;
    }
}
