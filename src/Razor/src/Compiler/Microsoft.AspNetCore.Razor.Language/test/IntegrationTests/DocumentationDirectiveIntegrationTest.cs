// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

[WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
public class DocumentationDirectiveIntegrationTest : RazorIntegrationTestBase
{
    private bool _enableMarkupSplit = true;
    private bool _declarationOnly;

    internal override RazorFileKind? FileKind => RazorFileKind.Component;
    internal override string DefaultFileName => "TestComponent.razor";
    internal override bool EnableMarkupSplit => _enableMarkupSplit;
    internal override bool DeclarationOnly => _declarationOnly;

    [Fact]
    public void DocumentationIsAttachedToComponentWithMarkupSplit()
    {
        var result = CompileToCSharp("""
            @documentation {
                <summary>
                Hello
                </summary>
                <remarks>More documentation.</remarks>
            }
            <p>Rendered content</p>
            """);

        Assert.Empty(result.RazorDiagnostics);
        var component = CompileToComponent(result, "Test.TestComponent");
        var documentation = GetDocumentation(component);
        Assert.Equal("Hello", Assert.Single(documentation.Elements("summary")).Value.Trim());
        Assert.Equal("More documentation.", Assert.Single(documentation.Elements("remarks")).Value);
        Assert.Single<RazorDocumentationDirectiveSyntax>(
            [.. result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);

        var declaration = result.CodeDocument.GetDeclCSharpDocument();
        Assert.NotNull(declaration);
        Assert.Contains("/**", declaration.Text.ToString());
        Assert.Contains("<p>Rendered content</p>", result.Code);
        Assert.DoesNotContain("<summary>", result.Code);
    }

    [Fact]
    public void DocumentationIsAttachedToComponentWithoutMarkupSplit()
    {
        _enableMarkupSplit = false;
        var result = CompileToCSharp("""
            @documentation {
                <summary>
                Hello
                </summary>
                <remarks>More documentation.</remarks>
            }
            <p>Rendered content</p>
            """);

        Assert.Empty(result.RazorDiagnostics);
        var component = CompileToComponent(result, "Test.TestComponent");
        var documentation = GetDocumentation(component);
        Assert.Equal("Hello", Assert.Single(documentation.Elements("summary")).Value.Trim());
        Assert.Equal("More documentation.", Assert.Single(documentation.Elements("remarks")).Value);
        Assert.Single<RazorDocumentationDirectiveSyntax>(
            [.. result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);

        Assert.Null(result.CodeDocument.GetDeclCSharpDocument());
        Assert.Contains("/**", result.Code);
        Assert.Contains("<p>Rendered content</p>", result.Code);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PlainTextDocumentationWarnsWithoutAddingSummary(bool split)
    {
        _enableMarkupSplit = split;
        var result = CompileToCSharp("""
            @documentation { This is the summary }
            <p>After</p>
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal("RZ1049", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(0, diagnostic.WarningLevel);
        Assert.Equal("Documentation should start with an XML tag, such as '<summary>'.", diagnostic.GetMessage());
        Assert.Equal(new SourceSpan(DefaultFileName, absoluteIndex: 17, lineIndex: 0, characterIndex: 17, length: 1), diagnostic.Span);
        Assert.Contains("/**  This is the summary */", GetDocumentationDocument(result).Text.ToString());
        Assert.Contains("<p>After</p>", result.Code);

        var documentation = GetDocumentation(CompileToComponent(result, "Test.TestComponent"));
        Assert.Equal("This is the summary", documentation.Value.Trim());
        Assert.Empty(documentation.Elements());
    }

    [Theory]
    [InlineData("", 0, 16)]
    [InlineData(" \t", 0, 18)]
    [InlineData("\u00a0\u2003", 0, 18)]
    [InlineData("\r\n    ", 1, 4)]
    [InlineData("\n \t", 1, 2)]
    [InlineData("\r \t", 1, 2)]
    [InlineData("\u0085 \t", 1, 2)]
    [InlineData("\u2028 \t", 1, 2)]
    [InlineData("\u2029 \t", 1, 2)]
    public void DocumentationPrefixWarningIgnoresLeadingWhitespace(string whitespace, int line, int character)
    {
        const string prefix = "@documentation {";
        var content = whitespace + "Plain text ";
        var result = CompileToCSharp(prefix + content + "}");

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal("RZ1049", diagnostic.Id);
        Assert.Equal(new SourceSpan(DefaultFileName, prefix.Length + whitespace.Length, line, character, length: 1), diagnostic.Span);

        var generated = GetDocumentationDocument(result);
        var mapping = Assert.Single(generated.SourceMappingsSortedByOriginal, mapping => mapping.OriginalSpan.AbsoluteIndex == prefix.Length);
        Assert.Equal(content, generated.Text.ToString(new TextSpan(mapping.GeneratedSpan.AbsoluteIndex, mapping.GeneratedSpan.Length)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    [InlineData("\u00a0\u2003\r\n\u0085\u2028\u2029")]
    [InlineData("<summary>Text</summary>")]
    [InlineData(" \t\r\n<summary>Text</summary>")]
    [InlineData("<!-- A comment -->")]
    [InlineData("<![CDATA[Plain text]]>")]
    [InlineData("<?example value=\"text\"?>")]
    [InlineData("<")]
    [InlineData("<summary")]
    public void EmptyOrXmlPrefixedDocumentationDoesNotWarn(string content)
    {
        var result = CompileToCSharp("@documentation {" + content + "}");

        Assert.Empty(result.RazorDiagnostics);
    }

    [Theory]
    [InlineData("@documentation {<summary>Hello</summary>}", true)]
    [InlineData("@documentation {<summary>Hello</summary>}", false)]
    [InlineData("@documentation {<summary>Hello</summary>\n}", true)]
    [InlineData("@documentation {<summary>Hello</summary>\n}", false)]
    [InlineData("@documentation {\n<summary>Hello</summary>}", true)]
    [InlineData("@documentation {\n<summary>Hello</summary>}", false)]
    [InlineData("@documentation\r\n{\r\n<summary>Hello</summary>\r\n}", true)]
    [InlineData("@documentation\r\n{\r\n<summary>Hello</summary>\r\n}", false)]
    [InlineData("@documentation\n{\n<summary>Hello</summary>\n}", true)]
    [InlineData("@documentation\n{\n<summary>Hello</summary>\n}", false)]
    public void SourceMappingPreservesXml(string source, bool enhanced)
    {
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions.WithLanguageVersion(
            enhanced ? LanguageVersion.Preview : LanguageVersion.CSharp9));
        Assert.Empty(result.RazorDiagnostics);
        CompileToComponent(result, "Test.TestComponent");

        var generated = GetDocumentationDocument(result);
        var start = source.IndexOf('{') + 1;
        var end = source.LastIndexOf('}');
        var mapping = Assert.Single(generated.SourceMappingsSortedByOriginal, mapping => mapping.OriginalSpan.AbsoluteIndex == start);
        Assert.Equal(end - start, mapping.OriginalSpan.Length);
        Assert.Equal(source[start..end], generated.Text.ToString(
            new TextSpan(mapping.GeneratedSpan.AbsoluteIndex, mapping.GeneratedSpan.Length)));
    }

    [Theory]
    [InlineData("@documentation {<summary>See <see cref=\"MissingType\"/>.</summary>}", true)]
    [InlineData("@documentation {<summary>See <see cref=\"MissingType\"/>.</summary>}", false)]
    [InlineData("@documentation {\n<summary>See <see cref=\"MissingType\"/>.</summary>\n}", true)]
    [InlineData("@documentation {\n<summary>See <see cref=\"MissingType\"/>.</summary>\n}", false)]
    public void DocumentationDiagnosticsMapToRazor(string source, bool enhanced)
    {
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions
            .WithLanguageVersion(enhanced ? LanguageVersion.Preview : LanguageVersion.CSharp9)
            .WithDocumentationMode(DocumentationMode.Diagnose));
        Assert.Empty(result.RazorDiagnostics);

        CompileToAssembly(result, diagnostics =>
        {
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("CS1574", diagnostic.Id);
            var mapped = diagnostic.Location.GetMappedLineSpan();
            Assert.Equal(DefaultDocumentPath, mapped.Path);
            Assert.Equal(
                result.CodeDocument.Source.Text.Lines.GetLinePosition(source.IndexOf("MissingType", StringComparison.Ordinal)),
                mapped.StartLinePosition);
        });
    }

    [Fact]
    public void DuplicateDocumentationReportsErrorsAndKeepsFirst()
    {
        const string source = """
            @documentation {<summary>First</summary>}
            @documentation {<summary>Second</summary>}
            @documentation {<remarks>Third</remarks>}
            """;
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Collection(result.RazorDiagnostics,
            diagnostic => VerifyDuplicate(diagnostic, source.IndexOf("@documentation", 1, StringComparison.Ordinal)),
            diagnostic => VerifyDuplicate(diagnostic, source.LastIndexOf("@documentation", StringComparison.Ordinal)));
        var documentation = GetDocumentation(CompileToComponent(result, "Test.TestComponent"));
        Assert.Equal("First", Assert.Single(documentation.Elements("summary")).Value);
        Assert.Empty(documentation.Elements("remarks"));
    }

    [Fact]
    public void DocumentationPrecedesAttributesAndSupportsGenericComponents()
    {
        var result = CompileToCSharp("""
            @attribute [System.Obsolete]
            @typeparam T
            @documentation {
                <summary>A <see cref="System.Collections.Generic.List{T}"/> of <typeparamref name="T"/>.</summary>
                <typeparam name="T">The item type.</typeparam>
            }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        var documentation = GetDocumentation(CompileToComponent(result, "Test.TestComponent`1"));
        var summary = Assert.Single(documentation.Elements("summary"));
        Assert.Equal("T", Assert.Single(summary.Elements("typeparamref")).Attribute("name")?.Value);
        Assert.Equal("The item type.", Assert.Single(documentation.Elements("typeparam")).Value);
    }

    [Fact]
    public void DocumentationIsEmittedForDeclarationOnly()
    {
        _declarationOnly = true;
        var component = CompileToComponent("@documentation {<summary>Declaration</summary>}");
        Assert.Equal("Declaration", Assert.Single(GetDocumentation(component).Elements("summary")).Value);
    }

    [Theory]
    [InlineData("<summary>It's \"quoted\", with @DateTime.Now, @* a comment *@, and }.</summary>")]
    [InlineData("<summary><![CDATA[<p>{ unmatched } }</p>]]></summary>")]
    [InlineData("<!-- } --> <summary>Braces</summary>")]
    [InlineData("<summary><see href=\"https://example.com/?x=}&amp;y=>\"/></summary>")]
    [InlineData("<summary><code>\nif (true)\n{\n}\n</code></summary>")]
    [InlineData("<summary>Text }\nMore text</summary>")]
    [InlineData("<summary>Text }\r\nMore text</summary>")]
    [InlineData("<summary>Text }\rMore text</summary>")]
    [InlineData("<summary>Text }\u0085More text</summary>")]
    [InlineData("<summary>Text }\u2028More text</summary>")]
    [InlineData("<summary>Text }\u2029More text</summary>")]
    [InlineData("<summary><see href=\"https://example.com/}\nmore\"/></summary>")]
    [InlineData("<!-- Text }\nMore text --> <summary>Braces</summary>")]
    [InlineData("<summary><![CDATA[Text }\nMore text]]></summary>")]
    [InlineData("<?example value=\"}\n\"?> <summary>Braces</summary>")]
    [InlineData("<summary>\n}\n@code { public string EndTag => \"&lt;/summary&gt;\"; }\n</summary>")]
    [InlineData("<summary>\n}\n@functions { public int Value => 42; }\n</summary>")]
    [InlineData("<summary><code>\n}\n@{ var value = 42; }\n</code></summary>")]
    [InlineData("<summary>\n}\n@using System.Text\n</summary>")]
    [InlineData("<summary>\n}\n@@code { }\n</summary>")]
    [InlineData("<summary><see href=\"}\n@code { }\"/></summary>")]
    [InlineData("<summary><!-- }\n@code { } -->After</summary>")]
    [InlineData("<summary><![CDATA[\n}\n@code { public string EndTag => \"</summary>\"; }\n]]></summary>")]
    [InlineData("<?example }\n@code { } ?> <summary>After</summary>")]
    [InlineData("<summary>\n}\n@code {\n</summary>")]
    [InlineData("<summary><!-- }\n@code { -->After</summary>")]
    [InlineData("<summary><![CDATA[\n}\n@code {\n]]></summary>")]
    [InlineData("<summary><see href=\"}\n@code { \"/></summary>")]
    [InlineData("<?example }\n@code { ?> <summary>After</summary>")]
    [InlineData("<summary>@value @@value @*value*@ }</summary>")]
    [InlineData("<summary><see href='@value @@value @*value*@ }'/></summary>")]
    [InlineData("<!-- @value @@value @*value*@ } --> <summary>After</summary>")]
    [InlineData("<![CDATA[@value @@value @*value*@ }]]> <summary>After</summary>")]
    [InlineData("<?example @value @@value @*value*@ } ?> <summary>After</summary>")]
    [InlineData("<!--> }\n@code { --> <summary>After</summary>")]
    [InlineData("<summary><![CDATA[\n[{\n}\n]]]></summary>")]
    public void XmlIsNotParsedAsRazorOrCSharp(string xml)
    {
        var source = "@documentation {" + xml + "}\n<p>After</p>";
        var result = CompileToCSharp(source);

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("/** " + xml + "*/", GetDocumentationDocument(result).Text.ToString());
        Assert.Contains("<p>After</p>", result.Code);
        CompileToComponent(result, "Test.TestComponent");
    }

    [Fact]
    public void XmlEntitiesDoNotBecomeDirectiveBraces()
    {
        var result = CompileToCSharp("""
            @documentation {&#123;<summary>&#125;</summary>}
            <p>After</p>
            @code { public int Value => 42; }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Equal("RZ1049", Assert.Single(result.RazorDiagnostics).Id);
        Assert.Contains("/** &#123;<summary>&#125;</summary>*/", GetDocumentationDocument(result).Text.ToString());
        Assert.Contains("<p>After</p>", result.Code);
        var component = CompileToComponent(result, "Test.TestComponent");
        Assert.Equal("}", Assert.Single(GetDocumentation(component).Elements("summary")).Value);
        Assert.Single(component.GetMembers("Value"));
    }

    [Fact]
    public void LeadingSlashesDoNotChangeDocumentationParsing()
    {
        var result = CompileToCSharp("""
            @documentation {/<summary>
            /// }
            </summary>}
            <p>After</p>
            @code { public int Value => 42; }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Equal("RZ1049", Assert.Single(result.RazorDiagnostics).Id);
        Assert.Contains("<p>After</p>", result.Code);
        var component = CompileToComponent(result, "Test.TestComponent");
        Assert.Equal("/// }", Assert.Single(GetDocumentation(component).Elements("summary")).Value.Trim());
        Assert.Single(component.GetMembers("Value"));
    }

    [Fact]
    public void DocumentationWithMixedLineEndingsPreservesSourceMapping()
    {
        const string xml = "<summary>\n}\r\n<!-- }\r -->\n<![CDATA[}\r\n]]></summary>";
        const string source = "<p>Before</p>\r\n@documentation {" + xml +
            "}<p>After</p>\r\n@code { public int Value => 42; }";
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>Before</p>", result.Code);
        Assert.Contains("<p>After</p>", result.Code);
        var generated = GetDocumentationDocument(result);
        var mapping = Assert.Single(generated.SourceMappingsSortedByOriginal,
            mapping => mapping.OriginalSpan.AbsoluteIndex == source.IndexOf('{') + 1);
        Assert.Equal(xml.Length, mapping.OriginalSpan.Length);
        Assert.Equal(xml, generated.Text.ToString(new TextSpan(mapping.GeneratedSpan.AbsoluteIndex, mapping.GeneratedSpan.Length)));
        Assert.Single(CompileToComponent(result, "Test.TestComponent").GetMembers("Value"));
    }

    [Theory]
    [InlineData(DocumentationMode.None)]
    [InlineData(DocumentationMode.Parse)]
    [InlineData(DocumentationMode.Diagnose)]
    public void DocumentationParsingDoesNotDependOnCSharpDocumentationMode(DocumentationMode documentationMode)
    {
        var result = CompileToCSharp("""
            @documentation {<summary>It's XML with } and @transitions.</summary>}
            <p>After</p>
            @code { public int Value => 42; }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(documentationMode));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("/** <summary>It's XML with } and @transitions.</summary>*/", GetDocumentationDocument(result).Text.ToString());
        Assert.Contains("<p>After</p>", result.Code);
        Assert.Single(CompileToComponent(result, "Test.TestComponent").GetMembers("Value"));
    }

    [Theory]
    [InlineData("@documentation", "RZ1012")]
    [InlineData("@documentation foo", "RZ1017")]
    [InlineData("@documentation <summary>Missing brace</summary>", "RZ1017")]
    [InlineData("@documentation {<summary>Missing end brace</summary>", "RZ1006")]
    [InlineData("@documentation {<summary>Missing end tag and brace", "RZ1006")]
    [InlineData("@documentation {<summary>A brace } followed by text", "RZ1006")]
    public void MissingBracesProduceRazorDiagnostics(string source, string diagnosticId)
    {
        var result = CompileToCSharp(source);
        Assert.Contains(result.RazorDiagnostics, diagnostic => diagnostic.Id == diagnosticId);
        Assert.Single<RazorDocumentationDirectiveSyntax>(
            [.. result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);
    }

    [Fact]
    public void MalformedXmlDoesNotConsumeFollowingMarkup()
    {
        var result = CompileToCSharp("""
            @documentation {
                <summary>Missing end tag
            }
            <p>After</p>
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        CompileToAssembly(result, diagnostics => Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "CS1570"));
    }

    [Theory]
    [InlineData("<summary>Missing end tag")]
    [InlineData("""<summary title="Missing quote""")]
    [InlineData("<!-- Missing end")]
    [InlineData("<![CDATA[Missing end")]
    [InlineData("<?example Missing end")]
    public void MalformedInlineXmlDoesNotConsumeFollowingMarkupOrCode(string xml)
    {
        var result = CompileToCSharp($$"""
            @documentation { {{xml}} }
            <p>After</p>
            @code { /* note */ public int Value => 42; }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        Assert.Contains("/* note */ public int Value => 42;", result.DeclCode);
        CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
    }

    [Fact]
    public void MalformedInlineXmlDoesNotConsumeFollowingMultilineCodeBlock()
    {
        var result = CompileToCSharp("""
            @documentation {<summary>Missing end tag}
            @code
            {
                /* note */
                public int Value => 42;
            }
            <p>After</p>
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        Assert.Contains("/* note */", result.DeclCode);
        Assert.Contains("public int Value => 42;", result.DeclCode);
        CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
    }

    [Theory]
    [InlineData("\"</summary>\"")]
    [InlineData("\"</div>\"")]
    [InlineData("@\"</summary>\"")]
    [InlineData("\"\"\"</summary>\"\"\"")]
    public void MalformedXmlDoesNotConsumeClosingTagsInFollowingCode(string expression)
    {
        var result = CompileToCSharp($$"""
            @documentation {<summary>Missing end tag}
            <p>After</p>
            @code { /* note */ public string EndTag => {{expression}}; }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        Assert.Contains("/* note */", result.DeclCode);
        var compiled = CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
        Assert.IsAssignableFrom<IPropertySymbol>(
            Assert.Single(CompileToComponent(compiled, "Test.TestComponent").GetMembers("EndTag")));
    }

    [Theory]
    [InlineData("<!-- Missing end", "\"-->\"")]
    [InlineData("<![CDATA[Missing end", "\"]]>\"")]
    [InlineData("<?example Missing end", "\"?>\"")]
    [InlineData("""<summary title="Missing quote""", "\"\\\"/>\"")]
    public void MalformedXmlDoesNotConsumeDelimitersInFollowingCode(string xml, string expression)
    {
        var result = CompileToCSharp($$"""
            @documentation { {{xml}} }
            <p>After</p>
            @code { public string EndMarker => {{expression}}; /* note */ }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        Assert.Contains("/* note */", result.DeclCode);
        var compiled = CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
        Assert.Single(CompileToComponent(compiled, "Test.TestComponent").GetMembers("EndMarker"));
    }

    [Theory]
    [InlineData("// </summary>")]
    [InlineData("/* </summary> */")]
    public void MalformedXmlDoesNotConsumeClosingTagsInFollowingCodeComments(string comment)
    {
        var result = CompileToCSharp($$"""
            @documentation {<summary>Missing end tag}
            <p>After</p>
            @code
            {
                {{comment}}
                public int Value => 42;
            }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        Assert.Contains(comment, result.DeclCode);
        var compiled = CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
        Assert.Single(CompileToComponent(compiled, "Test.TestComponent").GetMembers("Value"));
    }

    [Fact]
    public void MalformedXmlDoesNotConsumeClosingTagsInFollowingCodeWithoutComments()
    {
        var result = CompileToCSharp("""
            @documentation {<summary>Missing end tag}
            <p>After</p>
            @code { public string EndTag => "</summary>"; }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        var compiled = CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
        Assert.Single(CompileToComponent(compiled, "Test.TestComponent").GetMembers("EndTag"));
    }

    [Theory]
    [InlineData("<summary>Missing end tag")]
    [InlineData("""<summary title="Missing quote""")]
    [InlineData("<!-- Missing end")]
    [InlineData("<![CDATA[Missing end")]
    [InlineData("<?example Missing end")]
    public void MalformedXmlDoesNotConsumeSameLineMarkup(string xml)
    {
        var result = CompileToCSharp($$"""
            @documentation { {{xml}} }<p>After</p>
            @code { /* note */ public int Value => 42; }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        Assert.Contains("/* note */", result.DeclCode);
        var compiled = CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
        Assert.Single(CompileToComponent(compiled, "Test.TestComponent").GetMembers("Value"));
    }

    [Fact]
    public void MalformedXmlDoesNotConsumeFollowingAttribute()
    {
        var result = CompileToCSharp("""
            @documentation {<summary>Missing end tag}
            @attribute [System.Serializable]
            @code { public string EndTag => "</summary>"; }
            <p>After</p>
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        var compiled = CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
        var component = CompileToComponent(compiled, "Test.TestComponent");
        Assert.Single(component.GetMembers("EndTag"));
        Assert.Contains(component.GetAttributes(), attribute => attribute.AttributeClass?.ToDisplayString() == "System.SerializableAttribute");
    }

    [Fact]
    public void MalformedXmlDoesNotConsumeFollowingControlFlow()
    {
        var result = CompileToCSharp("""
            @documentation {<summary>Missing end tag}
            @if (true)
            {
                <p>After</p>
            }
            @code { /* note */ public string EndTag => "</summary>"; }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        Assert.Contains("if (true)", result.Code);
        var compiled = CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
        Assert.Single(CompileToComponent(compiled, "Test.TestComponent").GetMembers("EndTag"));
    }

    [Fact]
    public void MalformedXmlDoesNotConsumeFollowingDocumentationDirective()
    {
        var result = CompileToCSharp("""
            @documentation {<summary>Missing end tag}
            @documentation {<summary>Second block</summary>}
            @code { /* note */ public string EndTag => "</summary>"; }
            <p>After</p>
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Equal("RZ2001", Assert.Single(result.RazorDiagnostics).Id);
        Assert.Contains("<p>After</p>", result.Code);
        Assert.DoesNotContain("Second block", GetDocumentationDocument(result).Text.ToString());
        var compiled = CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
        Assert.Single(CompileToComponent(compiled, "Test.TestComponent").GetMembers("EndTag"));
    }

    [Fact]
    public void MalformedXmlRecoveryWithoutMarkupSplit()
    {
        _enableMarkupSplit = false;
        var result = CompileToCSharp("""
            @documentation {<summary>Missing end tag}<p>After</p>
            @code { /* note */ public string EndTag => "</summary>"; }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Null(result.DeclCode);
        Assert.Contains("<p>After</p>", result.Code);
        Assert.Contains("/* note */", result.Code);
        var compiled = CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
        Assert.Single(CompileToComponent(compiled, "Test.TestComponent").GetMembers("EndTag"));
    }

    [Fact]
    public void MalformedXmlRecoveryForDeclarationOnly()
    {
        _declarationOnly = true;
        var result = CompileToCSharp("""
            @documentation {<summary>Missing end tag}
            @code { /* note */ public string EndTag => "</summary>"; }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("/* note */", GetDocumentationDocument(result).Text.ToString());
        var compiled = CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
        Assert.Single(CompileToComponent(compiled, "Test.TestComponent").GetMembers("EndTag"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RecoveredDocumentationMappingStopsAtItsClosingBrace(bool enhanced)
    {
        const string source = """
            @documentation {<summary>Missing end tag}<p>After</p>
            @code { public string EndTag => "</summary>"; }
            """;
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions.WithLanguageVersion(
            enhanced ? LanguageVersion.Preview : LanguageVersion.CSharp9));

        Assert.Empty(result.RazorDiagnostics);
        var generated = GetDocumentationDocument(result);
        var mapping = Assert.Single(generated.SourceMappingsSortedByOriginal,
            mapping => mapping.OriginalSpan.AbsoluteIndex == source.IndexOf('{') + 1);
        Assert.Equal("<summary>Missing end tag".Length, mapping.OriginalSpan.Length);
        Assert.Equal("<summary>Missing end tag", generated.Text.ToString(
            new TextSpan(mapping.GeneratedSpan.AbsoluteIndex, mapping.GeneratedSpan.Length)));
        Assert.Contains("<p>After</p>", result.Code);
        Assert.Single(CompileToComponent(result, "Test.TestComponent").GetMembers("EndTag"));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    public void MalformedXmlRecoverySupportsDifferentLineEndings(string lineEnding)
    {
        var result = CompileToCSharp("""
            @documentation {<summary>Missing end tag}
            <p>After</p>
            @code { /* note */ public string EndTag => "</summary>"; }
            """.Replace("\r\n", "\n").Replace("\n", lineEnding),
            csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        var compiled = CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
        Assert.Single(CompileToComponent(compiled, "Test.TestComponent").GetMembers("EndTag"));
    }

    [Fact]
    public void DocumentationDiagnosticsMapPastIncompleteCodeExamples()
    {
        const string source = """
            @documentation {
                <summary>
                }
                @code {
                <see cref="MissingType"/>
                </summary>
            }
            <p>After</p>
            """;
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        CompileToAssembly(result, diagnostics =>
        {
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("CS1574", diagnostic.Id);
            var mapped = diagnostic.Location.GetMappedLineSpan();
            Assert.Equal(DefaultDocumentPath, mapped.Path);
            Assert.Equal(
                result.CodeDocument.Source.Text.Lines.GetLinePosition(source.IndexOf("MissingType", StringComparison.Ordinal)),
                mapped.StartLinePosition);
        });
    }

    [Fact]
    public void MissingDocumentationBraceAfterCompleteXmlDoesNotConsumeFollowingCode()
    {
        var result = CompileToCSharp("""
            @documentation {<summary>Complete XML, missing brace</summary>
            @code { /* note */ public int Value => 42; }
            <p>After</p>
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Equal("RZ1006", Assert.Single(result.RazorDiagnostics).Id);
        Assert.Contains("<p>After</p>", result.Code);
        Assert.Contains("/* note */", result.DeclCode);
        Assert.DoesNotContain("/**", GetDocumentationDocument(result).Text.ToString());
        Assert.Single(CompileToComponent(result, "Test.TestComponent").GetMembers("Value"));
    }

    [Fact]
    public void MissingDocumentationBraceDoesNotConsumeFollowingCode()
    {
        const string source = """
            @documentation {<summary>Missing end tag and brace
            @code
            {
                /* note */
                public string EndTag => "</summary>";
            }
            <p>After</p>
            """;
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal("RZ1006", diagnostic.Id);
        Assert.Equal(source.IndexOf('{'), diagnostic.Span.AbsoluteIndex);
        Assert.Contains("<p>After</p>", result.Code);
        Assert.Contains("/* note */", result.DeclCode);
        Assert.DoesNotContain("/**", GetDocumentationDocument(result).Text.ToString());
        Assert.Single(CompileToComponent(result, "Test.TestComponent").GetMembers("EndTag"));
    }

    [Fact]
    public void MissingDocumentationBraceDoesNotConsumeFollowingAttribute()
    {
        var result = CompileToCSharp("""
            @documentation {<summary>Missing end tag and brace
            @attribute [System.Serializable]
            @code { /* note */ public int Value => 42; }
            <p>After</p>
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Equal("RZ1006", Assert.Single(result.RazorDiagnostics).Id);
        Assert.Contains("<p>After</p>", result.Code);
        var component = CompileToComponent(result, "Test.TestComponent");
        Assert.Single(component.GetMembers("Value"));
        Assert.Contains(component.GetAttributes(), attribute => attribute.AttributeClass?.ToDisplayString() == "System.SerializableAttribute");
    }

    [Fact]
    public void CommentTerminatorDoesNotEndDocumentationParsing()
    {
        const string source = """
            @documentation {
                <summary>
                }
                @code { /* documentation example */ }
                </summary>
            }
            @code { /* actual code */ public int Value => 42; }
            """;
        var result = CompileToCSharp(source);

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal("RZ1047", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(source.IndexOf("*/", StringComparison.Ordinal), diagnostic.Span.AbsoluteIndex);
        Assert.Equal(2, diagnostic.Span.Length);
        var documentation = Assert.Single<RazorDocumentationDirectiveSyntax>(
            [.. result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);
        var body = Assert.Single<CSharpStatementLiteralSyntax>([.. documentation.DescendantNodes().OfType<CSharpStatementLiteralSyntax>()]);
        Assert.Equal(source[(source.IndexOf('{') + 1)..source.IndexOf('}', source.IndexOf("</summary>", StringComparison.Ordinal))], body.GetContent());
        Assert.DoesNotContain("documentation example", result.DeclCode);
        Assert.Contains("/* actual code */", result.DeclCode);
        var component = CompileToComponent(result, "Test.TestComponent");
        Assert.Single(component.GetMembers("Value"));
        Assert.Empty(GetDocumentationText(component));
    }

    [Fact]
    public void ClosingBraceInSummaryDoesNotEndDocumentation()
    {
        var result = CompileToCSharp("""
            @documentation {
                <summary>
                }
                @code { /* documentation example *&#47; }
                </summary>
            }
            @code { /* actual code */ public int Value => 42; }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        var component = CompileToComponent(result, "Test.TestComponent");
        var summary = Assert.Single(GetDocumentation(component).Elements("summary"));
        Assert.StartsWith("}", summary.Value.TrimStart());
        Assert.Contains("@code { /* documentation example */ }", summary.Value);
        Assert.Contains("/* actual code */", result.DeclCode);
        Assert.Single(component.GetMembers("Value"));
    }

    [Fact]
    public void CommentDecorationUsesOrdinaryRecovery()
    {
        const string source = """
            @documentation {
                * <summary>
                }
                @code { public int ExampleValue => 1; }
                * </summary>
            }
            <p>After</p>
            @code { public int Value => 42; }
            """;
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Collection(result.RazorDiagnostics,
            diagnostic =>
            {
                Assert.Equal("RZ1049", diagnostic.Id);
                Assert.Equal(source.IndexOf('*'), diagnostic.Span.AbsoluteIndex);
                Assert.Equal(1, diagnostic.Span.Length);
            },
            diagnostic =>
            {
                Assert.Equal("RZ9981", diagnostic.Id);
                Assert.Equal(source.IndexOf("</summary>", StringComparison.Ordinal), diagnostic.Span.AbsoluteIndex);
            });
        Assert.Contains("<p>After</p>", result.Code);
        var documentation = Assert.Single<RazorDocumentationDirectiveSyntax>(
            [.. result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);
        var body = Assert.Single<CSharpStatementLiteralSyntax>([.. documentation.DescendantNodes().OfType<CSharpStatementLiteralSyntax>()]);
        Assert.Equal(source[(source.IndexOf('{') + 1)..source.IndexOf('}')], body.GetContent());
        var compiled = CompileToAssembly(result, diagnostics =>
        {
            Assert.NotEmpty(diagnostics);
            Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
        });
        var component = CompileToComponent(compiled, "Test.TestComponent");
        Assert.Single(component.GetMembers("ExampleValue"));
        Assert.Single(component.GetMembers("Value"));
    }

    [Theory]
    [InlineData("param")]
    [InlineData("br")]
    [InlineData("script")]
    [InlineData("text")]
    [InlineData("SUMMARY")]
    public void DocumentationElementsDoNotUseHtmlTagRules(string tagName)
    {
        var result = CompileToCSharp($$"""
            @documentation {
                <{{tagName}}><{{tagName}}>Nested</{{tagName}}>
                }
                @code { public int ExampleValue => 1; }
                </{{tagName}}>
            }
            <p>After</p>
            @code { public int Value => 42; }
            """, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Parse));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        var component = CompileToComponent(result, "Test.TestComponent");
        var element = Assert.Single(GetDocumentation(component).Elements(tagName));
        Assert.Equal("Nested", Assert.Single(element.Elements(tagName)).Value);
        Assert.Contains("@code { public int ExampleValue => 1; }", element.Value);
        Assert.Empty(component.GetMembers("ExampleValue"));
        Assert.Single(component.GetMembers("Value"));
    }

    [Fact]
    public void ContinuedXmlTagsPreserveDocumentation()
    {
        const string source = """
            @documentation {
                <summary
                data-example="@value @@value @*value*@ }">
                }
                @code { public int ExampleValue => 1; }
                </summary
                >
            }
            <p>After</p>
            @code { public int Value => 42; }
            """;
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Contains("<p>After</p>", result.Code);
        var generated = GetDocumentationDocument(result);
        var mapping = Assert.Single(generated.SourceMappingsSortedByOriginal,
            mapping => mapping.OriginalSpan.AbsoluteIndex == source.IndexOf('{') + 1);
        Assert.Equal(source.Substring(mapping.OriginalSpan.AbsoluteIndex, mapping.OriginalSpan.Length),
            generated.Text.ToString(new TextSpan(mapping.GeneratedSpan.AbsoluteIndex, mapping.GeneratedSpan.Length)));
        var component = CompileToComponent(result, "Test.TestComponent");
        var summary = Assert.Single(GetDocumentation(component).Elements("summary"));
        Assert.Equal("@value @@value @*value*@ }", summary.Attribute("data-example")?.Value);
        Assert.Contains("@code { public int ExampleValue => 1; }", summary.Value);
        Assert.Empty(component.GetMembers("ExampleValue"));
        Assert.Single(component.GetMembers("Value"));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    public void DocumentationPreservesRawSourceAndFollowingMarkup(string lineEnding)
    {
        var documentationSource = """
            @documentation {
                <summary>Multiplication uses *.</summary>
            }
            """.Replace("\r\n", "\n").Replace("\n", lineEnding);
        var source = $$"""
            *** Before
            {{documentationSource}}
            *** After
            @code { public int Value => 42; }
            """;
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        Assert.Equal(source, result.CodeDocument.Source.Text.ToString());
        Assert.Contains("*** Before", result.Code);
        Assert.Contains("*** After", result.Code);
        var documentation = Assert.Single<RazorDocumentationDirectiveSyntax>(
            [.. result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);
        var body = Assert.Single<CSharpStatementLiteralSyntax>([.. documentation.DescendantNodes().OfType<CSharpStatementLiteralSyntax>()]);
        var start = source.IndexOf('{') + 1;
        var end = source.IndexOf('}', start);
        Assert.Equal(source[start..end], body.GetContent());
        var generated = GetDocumentationDocument(result);
        var mapping = Assert.Single(generated.SourceMappingsSortedByOriginal, mapping => mapping.OriginalSpan.AbsoluteIndex == start);
        Assert.Equal(end - start, mapping.OriginalSpan.Length);
        Assert.Equal(source[start..end],
            generated.Text.ToString(new TextSpan(mapping.GeneratedSpan.AbsoluteIndex, mapping.GeneratedSpan.Length)));
        var component = CompileToComponent(result, "Test.TestComponent");
        Assert.Equal("Multiplication uses *.", Assert.Single(GetDocumentation(component).Elements("summary")).Value.Trim());
        Assert.Single(component.GetMembers("Value"));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("***")]
    public void LineLeadingCommentTerminatorReportsItsOwnLocation(string stars)
    {
        var source = $$"""
            @documentation {
                <summary>
                {{stars}}/
                </summary>
            }
            @code { public int Value => 42; }
            """;
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal("RZ1047", diagnostic.Id);
        Assert.Equal(source.IndexOf("*/", StringComparison.Ordinal), diagnostic.Span.AbsoluteIndex);
        Assert.Equal(2, diagnostic.Span.Length);
        var component = CompileToComponent(result, "Test.TestComponent");
        Assert.Empty(GetDocumentationText(component));
        Assert.Single(component.GetMembers("Value"));
    }

    [Fact]
    public void CommentTerminatorBeforeClosingXmlTagReportsItsOwnLocation()
    {
        const string source = """
            @documentation {
                <summary>/* example */</summary>
            }
            """;
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal("RZ1047", diagnostic.Id);
        Assert.Equal(source.IndexOf("*/", StringComparison.Ordinal), diagnostic.Span.AbsoluteIndex);
        Assert.Equal(2, diagnostic.Span.Length);
        Assert.DoesNotContain("/* example", GetDocumentationDocument(result).Text.ToString());
        Assert.Empty(GetDocumentationText(CompileToComponent(result, "Test.TestComponent")));
    }

    [Fact]
    public void CommentTerminatorAfterCompleteXmlReportsItsOwnLocation()
    {
        const string source = """
            @documentation {<summary>Complete XML</summary>*/ [System.Obsolete] /*}
            @code { public int Value => 42; }
            """;
        var result = CompileToCSharp(source, csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal("RZ1047", diagnostic.Id);
        Assert.Equal(source.IndexOf("*/", StringComparison.Ordinal), diagnostic.Span.AbsoluteIndex);
        Assert.Equal(2, diagnostic.Span.Length);
        Assert.DoesNotContain("Obsolete", GetDocumentationDocument(result).Text.ToString());
        var component = CompileToComponent(result, "Test.TestComponent");
        Assert.Empty(GetDocumentationText(component));
        Assert.Single(component.GetMembers("Value"));
    }

    [Fact]
    public void CommentTerminatorCannotEscapeIntoGeneratedCode()
    {
        var result = CompileToCSharp("""
            @documentation {<summary>*/ [System.Obsolete] /*</summary>}
            @documentation {<summary>Ignored</summary>}
            """);

        Assert.Contains(result.RazorDiagnostics, diagnostic => diagnostic.Id == "RZ1047");
        Assert.Contains(result.RazorDiagnostics, diagnostic => diagnostic.Id == "RZ2001");
        var generated = GetDocumentationDocument(result).Text.ToString();
        Assert.DoesNotContain("Obsolete", generated);
        Assert.DoesNotContain("Ignored", generated);
        Assert.Empty(GetDocumentationText(CompileToComponent(result, "Test.TestComponent")));
    }

    [Theory]
    [InlineData("Plain text */")]
    [InlineData("*/")]
    [InlineData("***/")]
    public void CommentTerminatorSuppressesDocumentationPrefixWarning(string content)
    {
        var source = "@documentation {" + content + "}";
        var result = CompileToCSharp(source);

        var diagnostic = Assert.Single(result.RazorDiagnostics);
        Assert.Equal("RZ1047", diagnostic.Id);
        Assert.Equal(source.IndexOf("*/", StringComparison.Ordinal), diagnostic.Span.AbsoluteIndex);
        Assert.Equal(2, diagnostic.Span.Length);
        Assert.Empty(GetDocumentationText(CompileToComponent(result, "Test.TestComponent")));
    }

    [Theory]
    [InlineData("/ [System.Obsolete] /*")]
    [InlineData("*")]
    public void ContentCannotChangeTheCommentOpener(string prefix)
    {
        var result = CompileToCSharp("@documentation {" + prefix + " <summary>Documentation</summary>}",
            csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Equal("RZ1049", Assert.Single(result.RazorDiagnostics).Id);
        var component = CompileToComponent(result, "Test.TestComponent");
        Assert.Equal("Documentation", Assert.Single(GetDocumentation(component).Elements("summary")).Value);
        Assert.DoesNotContain(component.GetAttributes(), attribute => attribute.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute");
    }

    [Fact]
    public void DocumentationCannotBeImported()
    {
        ImportItems.Add(CreateProjectItem("_Imports.razor",
            "@documentation {<summary>Imported</summary>}", RazorFileKind.ComponentImport));
        var result = CompileToCSharp("@documentation {<summary>Local</summary>}");

        Assert.Contains(result.RazorDiagnostics, diagnostic => diagnostic.Id == "RZ0000");
        Assert.Equal("Local", Assert.Single(GetDocumentation(CompileToComponent(result, "Test.TestComponent")).Elements("summary")).Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ImportedDocumentationWithMissingBodyPreservesLocalDocumentation(bool enableMarkupSplit)
    {
        _enableMarkupSplit = enableMarkupSplit;
        ImportItems.Add(CreateProjectItem("_Imports.razor",
            "@documentation", RazorFileKind.ComponentImport));
        var result = CompileToCSharp("@documentation {<summary>Local</summary>}");

        Assert.Contains(result.RazorDiagnostics, diagnostic => diagnostic.Id == "RZ1012");
        Assert.Equal("Local", Assert.Single(GetDocumentation(CompileToComponent(result, "Test.TestComponent")).Elements("summary")).Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ImportedDocumentationWithCommentTerminatorPreservesLocalDocumentation(bool enableMarkupSplit)
    {
        _enableMarkupSplit = enableMarkupSplit;
        ImportItems.Add(CreateProjectItem("_Imports.razor",
            "@documentation {<summary>Imported */</summary>}", RazorFileKind.ComponentImport));
        var result = CompileToCSharp("@documentation {<summary>Local</summary>}");

        Assert.Contains(result.RazorDiagnostics, diagnostic => diagnostic.Id == "RZ1047");
        Assert.Equal("Local", Assert.Single(GetDocumentation(CompileToComponent(result, "Test.TestComponent")).Elements("summary")).Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ImportedDocumentationDoesNotOverrideEmptyLocalDocumentation(bool enableMarkupSplit)
    {
        _enableMarkupSplit = enableMarkupSplit;
        ImportItems.Add(CreateProjectItem("_Imports.razor",
            "@documentation", RazorFileKind.ComponentImport));
        var result = CompileToCSharp("""
            @documentation {}
            @documentation {<summary>Ignored</summary>}
            """);

        Assert.Contains(result.RazorDiagnostics, diagnostic => diagnostic.Id == "RZ1012");
        Assert.Contains(result.RazorDiagnostics, diagnostic => diagnostic.Id == "RZ2001");
        Assert.Empty(GetDocumentationText(CompileToComponent(result, "Test.TestComponent")));
    }

    [Theory]
    [InlineData("<summary>/* example *&#47;</summary>")]
    [InlineData("<summary><![CDATA[/* example *]]>&#47;<![CDATA[]]></summary>")]
    public void EscapedCommentTerminatorPreservesDocumentation(string xml)
    {
        var result = CompileToCSharp("@documentation {" + xml + "}",
            csharpParseOptions: CSharpParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

        Assert.Empty(result.RazorDiagnostics);
        var component = CompileToComponent(result, "Test.TestComponent");
        Assert.Equal("/* example */", Assert.Single(GetDocumentation(component).Elements("summary")).Value);
    }

    [Theory]
    [InlineData("9.0")]
    [InlineData("11.0")]
    public void DocumentationRequiresRazor12(string languageVersion)
    {
        var result = CompileToCSharp("@documentation {<summary>Not a directive</summary>}",
            configuration: Configuration with { LanguageVersion = RazorLanguageVersion.Parse(languageVersion) });

        Assert.Null(result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>().FirstOrDefault());
    }

    [Theory]
    [CombinatorialData]
    public void DocumentationSupportsBothTokenizersAndDeclarationGeneration(bool useRoslynTokenizer, bool declarationOnly)
    {
        const string source = """
            @documentation {
                <summary>It's raw XML with @transitions and }.</summary>
            }
            <p>After</p>
            """;
        var engine = RazorProjectEngine.Create(Configuration, FileSystem, builder =>
        {
            builder.ConfigureParserOptions(options => options.UseRoslynTokenizer = useRoslynTokenizer);
        });
        var item = CreateProjectItem(DefaultFileName, source);
        var document = declarationOnly ? engine.ProcessDeclarationOnly(item) : engine.Process(item);

        Assert.Empty(document.GetRequiredImplCSharpDocument().Diagnostics);
        Assert.Equal(source, document.GetRequiredSyntaxTree().Root.GetContent());
        Assert.Contains("/**", document.GetRequiredImplCSharpDocument().Text.ToString());
        var html = RazorHtmlWriter.GetHtmlDocument(document);
        Assert.DoesNotContain("<summary>", html.Text.ToString());
        Assert.Contains("<p>After</p>", html.Text.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DocumentationModeDoesNotAffectFollowingRazor(bool useRoslynTokenizer)
    {
        const string source = """
            @documentation {
                <summary data-example="@Value @@ @*literal*@">} @Value @@ @*literal*@</summary>
            }
            @* ordinary Razor comment *@
            <p title="@Value">@@ @Value</p>
            @code { public int Value => 42; }
            """;
        var engine = RazorProjectEngine.Create(Configuration, FileSystem, builder =>
        {
            builder.ConfigureParserOptions(options => options.UseRoslynTokenizer = useRoslynTokenizer);
        });
        var document = engine.Process(CreateProjectItem(DefaultFileName, source));
        var root = document.GetRequiredSyntaxTree().Root;

        Assert.Empty(document.GetRequiredImplCSharpDocument().Diagnostics);
        Assert.Equal(source, root.GetContent());
        Assert.Single<RazorDocumentationDirectiveSyntax>([.. root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);
        Assert.Single<RazorCommentBlockSyntax>([.. root.DescendantNodes().OfType<RazorCommentBlockSyntax>()]);
        Assert.Collection<CSharpImplicitExpressionSyntax>([.. root.DescendantNodes().OfType<CSharpImplicitExpressionSyntax>()],
            expression => Assert.Equal("@Value", expression.GetContent()),
            expression => Assert.Equal("@Value", expression.GetContent()));
        Assert.Contains("public int Value => 42;", document.GetRequiredImplCSharpDocument().Text.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OverlappingCDataTerminatorPreservesDocumentation(bool useRoslynTokenizer)
    {
        const string xml = "\n<summary><![CDATA[\n[{\n}\n]]]></summary>\n";
        var source = "@documentation {" + xml + "}\n<p>After</p>\n@code { public int Value => 42; }";
        var engine = RazorProjectEngine.Create(Configuration, FileSystem, builder =>
        {
            builder.ConfigureParserOptions(options => options.UseRoslynTokenizer = useRoslynTokenizer);
        });
        var document = engine.Process(CreateProjectItem(DefaultFileName, source));
        var root = document.GetRequiredSyntaxTree().Root;
        var generated = document.GetRequiredImplCSharpDocument();

        Assert.Empty(generated.Diagnostics);
        Assert.Equal(source, root.GetContent());
        var directive = Assert.Single<RazorDocumentationDirectiveSyntax>(
            [.. root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);
        var body = Assert.Single<CSharpStatementLiteralSyntax>(
            [.. directive.DescendantNodes().OfType<CSharpStatementLiteralSyntax>()]);
        Assert.Equal(xml, body.GetContent());
        Assert.Contains("/** " + xml + "*/", generated.Text.ToString());
        Assert.Contains("<p>After</p>", generated.Text.ToString());
        Assert.Contains("public int Value => 42;", generated.Text.ToString());
    }

    [Fact]
    public void EmptyDocumentationDoesNotEmitInvalidMapping()
    {
        var result = CompileToCSharp("@documentation {}");
        Assert.Empty(result.RazorDiagnostics);
        Assert.Empty(GetDocumentationText(CompileToComponent(result, "Test.TestComponent")));
    }

    private static RazorCSharpDocument GetDocumentationDocument(CompileToCSharpResult result)
        => result.CodeDocument.GetDeclCSharpDocument() ?? result.CodeDocument.GetRequiredImplCSharpDocument();

    private static string GetDocumentationText(INamedTypeSymbol component)
        => component.GetDocumentationCommentXml() ?? string.Empty;

    private static XElement GetDocumentation(INamedTypeSymbol component)
        => Assert.IsType<XElement>(XDocument.Parse(GetDocumentationText(component)).Root);

    private static void VerifyDuplicate(RazorDiagnostic diagnostic, int start)
    {
        Assert.Equal("RZ2001", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("The 'documentation' directive may only occur once per document.", diagnostic.GetMessage());
        Assert.Equal(start, diagnostic.Span.AbsoluteIndex);
        Assert.Equal("@documentation".Length, diagnostic.Span.Length);
    }
}
