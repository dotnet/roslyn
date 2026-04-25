// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class HtmlAttributeTest() : ParserTestBase(layer: TestProject.Layer.Compiler)
{
    [Fact]
    public void SymbolBoundAttributes_BeforeEqualWhitespace1()
    {
        var attributeName = "[item]";
        ParseDocumentTest($$"""
            @{<a {{attributeName}}
            ='Foo'	{{attributeName}}=
            'Bar' />}
            """);
    }

    [Fact]
    public void SymbolBoundAttributes_BeforeEqualWhitespace2()
    {
        var attributeName = "[(item,";
        ParseDocumentTest($$"""
            @{<a {{attributeName}}
            ='Foo'	{{attributeName}}=
            'Bar' />}
            """);
    }

    [Fact]
    public void SymbolBoundAttributes_BeforeEqualWhitespace3()
    {
        var attributeName = "(click)";
        ParseDocumentTest($$"""
            @{<a {{attributeName}}
            ='Foo'	{{attributeName}}=
            'Bar' />}
            """);
    }

    [Fact]
    public void SymbolBoundAttributes_BeforeEqualWhitespace4()
    {
        var attributeName = "(^click)";
        ParseDocumentTest($$"""
            @{<a {{attributeName}}
            ='Foo'	{{attributeName}}=
            'Bar' />}
            """);
    }

    [Fact]
    public void SymbolBoundAttributes_BeforeEqualWhitespace5()
    {
        var attributeName = "*something";
        ParseDocumentTest($$"""
            @{<a {{attributeName}}
            ='Foo'	{{attributeName}}=
            'Bar' />}
            """);
    }

    [Fact]
    public void SymbolBoundAttributes_BeforeEqualWhitespace6()
    {
        var attributeName = "#local";
        ParseDocumentTest($$"""
            @{<a {{attributeName}}
            ='Foo'	{{attributeName}}=
            'Bar' />}
            """);
    }

    [Fact]
    public void SymbolBoundAttributes_Whitespace1()
    {
        var attributeName = "[item]";
        ParseDocumentTest($$"""
            @{<a 
              {{attributeName}}='Foo'	
            {{attributeName}}='Bar' />}
            """);
    }

    [Fact]
    public void SymbolBoundAttributes_Whitespace2()
    {
        var attributeName = "[(item,";
        ParseDocumentTest($$"""
            @{<a 
              {{attributeName}}='Foo'	
            {{attributeName}}='Bar' />}
            """);
    }

    [Fact]
    public void SymbolBoundAttributes_Whitespace3()
    {
        var attributeName = "(click)";
        ParseDocumentTest($$"""
            @{<a 
              {{attributeName}}='Foo'	
            {{attributeName}}='Bar' />}
            """);
    }

    [Fact]
    public void SymbolBoundAttributes_Whitespace4()
    {
        var attributeName = "(^click)";
        ParseDocumentTest($$"""
            @{<a 
              {{attributeName}}='Foo'	
            {{attributeName}}='Bar' />}
            """);
    }

    [Fact]
    public void SymbolBoundAttributes_Whitespace5()
    {
        var attributeName = "*something";
        ParseDocumentTest($$"""
            @{<a 
              {{attributeName}}='Foo'	
            {{attributeName}}='Bar' />}
            """);
    }

    [Fact]
    public void SymbolBoundAttributes_Whitespace6()
    {
        var attributeName = "#local";
        ParseDocumentTest($$"""
            @{<a 
              {{attributeName}}='Foo'	
            {{attributeName}}='Bar' />}
            """);
    }

    [Fact]
    public void SymbolBoundAttributes1()
    {
        var attributeName = "[item]";
        ParseDocumentTest($$"""@{<a {{attributeName}}='Foo' />}""");
    }

    [Fact]
    public void SymbolBoundAttributes2()
    {
        var attributeName = "[(item,";
        ParseDocumentTest($$"""@{<a {{attributeName}}='Foo' />}""");
    }

    [Fact]
    public void SymbolBoundAttributes3()
    {
        var attributeName = "(click)";
        ParseDocumentTest($$"""@{<a {{attributeName}}='Foo' />}""");
    }

    [Fact]
    public void SymbolBoundAttributes4()
    {
        var attributeName = "(^click)";
        ParseDocumentTest($$"""@{<a {{attributeName}}='Foo' />}""");
    }

    [Fact]
    public void SymbolBoundAttributes5()
    {
        var attributeName = "*something";
        ParseDocumentTest($$"""@{<a {{attributeName}}='Foo' />}""");
    }

    [Fact]
    public void SymbolBoundAttributes6()
    {
        var attributeName = "#local";
        ParseDocumentTest($$"""@{<a {{attributeName}}='Foo' />}""");
    }

    [Fact]
    public void SimpleLiteralAttribute()
    {
        ParseDocumentTest("@{<a href='Foo' />}");
    }

    [Fact]
    public void SimpleLiteralAttributeWithWhitespaceSurroundingEquals()
    {
        ParseDocumentTest("@{<a href \f\r\n= \t\n'Foo' />}");
    }

    [Fact]
    public void DynamicAttributeWithWhitespaceSurroundingEquals()
    {
        ParseDocumentTest("@{<a href \n= \r\n'@Foo' />}");
    }

    [Fact]
    public void MultiPartLiteralAttribute()
    {
        ParseDocumentTest("@{<a href='Foo Bar Baz' />}");
    }

    [Fact]
    public void DoubleQuotedLiteralAttribute()
    {
        ParseDocumentTest("@{<a href=\"Foo Bar Baz\" />}");
    }

    [Fact]
    public void NewLinePrecedingAttribute()
    {
        ParseDocumentTest("@{<a\r\nhref='Foo' />}");
    }

    [Fact]
    public void NewLineBetweenAttributes()
    {
        ParseDocumentTest("@{<a\nhref='Foo'\r\nabcd='Bar' />}");
    }

    [Fact]
    public void WhitespaceAndNewLinePrecedingAttribute()
    {
        ParseDocumentTest("@{<a \t\r\nhref='Foo' />}");
    }

    [Fact]
    public void UnquotedLiteralAttribute()
    {
        ParseDocumentTest("@{<a href=Foo Bar Baz />}");
    }

    [Fact]
    public void SimpleExpressionAttribute()
    {
        ParseDocumentTest("@{<a href='@foo' />}");
    }

    [Fact]
    public void MultiValueExpressionAttribute()
    {
        ParseDocumentTest("@{<a href='@foo bar @baz' />}");
    }

    [Fact]
    public void VirtualPathAttributesWorkWithConditionalAttributes()
    {
        ParseDocumentTest("@{<a href='@foo ~/Foo/Bar' />}");
    }

    [Fact]
    public void UnquotedAttributeWithCodeWithSpacesInBlock()
    {
        ParseDocumentTest("@{<input value=@foo />}");
    }

    [Fact]
    public void UnquotedAttributeWithCodeWithSpacesInDocument()
    {
        ParseDocumentTest("<input value=@foo />}");
    }

    [Fact]
    public void ConditionalAttributesAreEnabledForDataAttributesWithExperimentalFlag()
    {
        ParseDocumentTest(RazorLanguageVersion.Experimental, "@{<span data-foo='@foo'></span>}", directives: default, designTime: false);
    }

    [Fact]
    public void ConditionalAttributesAreDisabledForDataAttributesInBlock()
    {
        ParseDocumentTest("@{<span data-foo='@foo'></span>}");
    }

    [Fact]
    public void ConditionalAttributesWithWeirdSpacingAreDisabledForDataAttributesInBlock()
    {
        ParseDocumentTest("@{<span data-foo  =  '@foo'></span>}");
    }

    [Fact]
    public void ConditionalAttributesAreDisabledForDataAttributesInDocument()
    {
        ParseDocumentTest("@{<span data-foo='@foo'></span>}");
    }

    [Fact]
    public void ConditionalAttributesWithWeirdSpacingAreDisabledForDataAttributesInDocument()
    {
        ParseDocumentTest("@{<span data-foo=@foo ></span>}");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10586")]
    public void ConditionalAttribute_DynamicContentAfter()
    {
        ParseDocumentTest("""<p class="@c" @x />""");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10586")]
    public void ConditionalAttribute_DynamicContentBefore()
    {
        ParseDocumentTest("""<p @x class="@c" />""");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10586")]
    public void ConditionalAttribute_DynamicContentBefore_02()
    {
        ParseDocumentTest("""<p @(x + y) class="@c" />""");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10586")]
    public void ConditionalAttribute_DynamicContentBefore_03()
    {
        ParseDocumentTest("""<p @{if (x) { @x }} class="@c" />""");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10586")]
    public void ConditionalAttribute_DynamicContentBefore_04()
    {
        ParseDocumentTest("""<p @@x class="@c" />""");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10586")]
    public void ConditionalAttribute_InvalidContentBefore()
    {
        ParseDocumentTest("""<p "ab" class="@c" />""");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10586")]
    public void ConditionalAttribute_CommentAfter()
    {
        ParseDocumentTest("""<p class="@c" @* comment *@ />""");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10586")]
    public void ConditionalAttribute_CommentBefore()
    {
        ParseDocumentTest("""<p @* comment *@ class="@c" />""");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/12261")]
    public void AttributeAfterComment()
    {
        ParseDocumentTest("""<p class="first" @* comment *@ data-value="second" />""");
    }

    [Fact]
    public void EscapedAttributeName_WithValue()
    {
        ParseDocumentTest("""<p @@attr="value" />""");
    }

    [Fact]
    public void EscapedAttributeName_Minimized()
    {
        ParseDocumentTest("""<p @@attr />""");
    }

    [Fact]
    public void EscapedAttributeName_Eof()
    {
        ParseDocumentTest("""<p @@""");
    }

    [Fact]
    public void EscapedAttributeName_InvalidName()
    {
        ParseDocumentTest("""<p @@"invalid" />""");
    }

    [Fact]
    public void ComponentFileKind_ParsesDirectiveAttributesAsMarkup()
    {
        ParseDocumentTest("<span @class='@foo'></span>", RazorFileKind.Component);
    }

    [Fact]
    public void ComponentFileKind_ParsesDirectiveAttributesWithParameterAsMarkup()
    {
        ParseDocumentTest("<span @class:param='@foo'></span>", RazorFileKind.Component);
    }

    [Fact]
    public void EscapedAttributeValue_InHtmlElement()
    {
        ParseDocumentTest("""<p class="@@test">Content</p>""");
    }

    [Fact]
    public void EscapedAttributeValue_InComponent()
    {
        ParseDocumentTest("""<Weather Value="@@currentCount" />""", RazorFileKind.Component);
    }

    [Fact]
    public void EscapedAttributeValue_MultipleInComponent()
    {
        ParseDocumentTest("""<Weather Value="@@count" Title="@@title" />""", RazorFileKind.Component);
    }

    [Fact]
    public void EscapedAttributeValue_MixedWithCSharp()
    {
        ParseDocumentTest("""<Weather Value="@@currentCount @someVar" />""", RazorFileKind.Component);
    }
}
