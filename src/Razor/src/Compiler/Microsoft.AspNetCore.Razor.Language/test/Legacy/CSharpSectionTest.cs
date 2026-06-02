// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpSectionTest() : ParserTestBase(layer: TestProject.Layer.Compiler)
{
    [Fact]
    public void CapturesNewlineImmediatelyFollowing()
    {
        ParseDocumentTest("""
            @section

            """,
            [SectionDirective.Directive]);
    }

    [Fact]
    public void CapturesWhitespaceToEndOfLineInSectionStatementMissingOpenBrace()
    {
        ParseDocumentTest("""
            @section Foo         
                
            """,
            [SectionDirective.Directive]);
    }

    [Fact]
    public void CapturesWhitespaceToEndOfLineInSectionStatementMissingName()
    {
        ParseDocumentTest("""
            @section         
                
            """,
            [SectionDirective.Directive]);
    }

    [Fact]
    public void IgnoresSectionUnlessAllLowerCase()
    {
        ParseDocumentTest(
            "@Section foo",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void ReportsErrorAndTerminatesSectionBlockIfKeywordNotFollowedByIdentifierStartChar()
    {
        // ParseSectionBlockReportsErrorAndTerminatesSectionBlockIfKeywordNotFollowedByIdentifierStartCharacter
        ParseDocumentTest(
            "@section 9 { <p>Foo</p> }",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void ReportsErrorAndTerminatesSectionBlockIfNameNotFollowedByOpenBrace()
    {
        // ParseSectionBlockReportsErrorAndTerminatesSectionBlockIfNameNotFollowedByOpenBrace
        ParseDocumentTest(
            "@section foo-bar { <p>Foo</p> }",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void ParserOutputsErrorOnNestedSections()
    {
        ParseDocumentTest(
            "@section foo { @section bar { <p>Foo</p> } }",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void ParserOutputsErrorOnMultipleNestedSections()
    {
        ParseDocumentTest(
            "@section foo { @section bar { <p>Foo</p> @section baz { } } }",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void ParserDoesNotOutputErrorOtherNestedDirectives()
    {
        // This isn't a real scenario but we just want to verify we don't show misleading errors.
        ParseDocumentTest(
            "@section foo { @inherits Bar }",
            [SectionDirective.Directive, InheritsDirective.Directive]);
    }

    [Fact]
    public void HandlesEOFAfterOpenBrace()
    {
        ParseDocumentTest(
            "@section foo {",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void HandlesEOFAfterOpenContent1()
    {

        ParseDocumentTest(
            "@section foo { ",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void HandlesEOFAfterOpenContent2()
    {

        ParseDocumentTest(
            "@section foo {\n",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void HandlesEOFAfterOpenContent3()
    {

        ParseDocumentTest(
            "@section foo {abc",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void HandlesEOFAfterOpenContent4()
    {

        ParseDocumentTest(
            "@section foo {\n abc",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void HandlesUnterminatedSection()
    {
        ParseDocumentTest(
            "@section foo { <p>Foo{}</p>",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void HandlesUnterminatedSectionWithNestedIf()
    {
        // Arrange
        var newLine = Environment.NewLine;
        var spaces = "    ";

        // Act & Assert
        ParseDocumentTest(
            string.Format(
                CultureInfo.InvariantCulture,
                "@section Test{0}{{{0}{1}@if(true){0}{1}{{{0}{1}{1}<p>Hello World</p>{0}{1}}}",
                newLine,
                spaces),
            [SectionDirective.Directive]);
    }

    [Fact]
    public void ReportsErrorAndAcceptsWhitespaceToEOLIfSectionNotFollowedByOpenBrace()
    {
        // ParseSectionBlockReportsErrorAndAcceptsWhitespaceToEndOfLineIfSectionNotFollowedByOpenBrace
        ParseDocumentTest("""
            @section foo      

            """,
            [SectionDirective.Directive]);
    }

    [Fact]
    public void AcceptsOpenBraceMultipleLinesBelowSectionName()
    {
        ParseDocumentTest("""
            @section foo      





            {
            <p>Foo</p>
            }
            """,
            [SectionDirective.Directive]);
    }

    [Fact]
    public void ParsesNamedSectionCorrectly()
    {
        ParseDocumentTest(
            "@section foo { <p>Foo</p> }",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void DoesNotRequireSpaceBetweenSectionNameAndOpenBrace()
    {
        ParseDocumentTest(
            "@section foo{ <p>Foo</p> }",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void BalancesBraces()
    {
        ParseDocumentTest(
            "@section foo { <script>(function foo() { return 1; })();</script> }",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void AllowsBracesInCSharpExpression()
    {
        ParseDocumentTest(
            "@section foo { I really want to render a close brace, so here I go: @(\"}\") }",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void SectionIsCorrectlyTerminatedWhenCloseBraceImmediatelyFollowsCodeBlock()
    {
        ParseDocumentTest("""
            @section Foo {
            @if(true) {
            }
            }
            """,
            [SectionDirective.Directive]);
    }

    [Fact]
    public void SectionCorrectlyTerminatedWhenCloseBraceFollowsCodeBlockNoWhitespace()
    {
        // SectionIsCorrectlyTerminatedWhenCloseBraceImmediatelyFollowsCodeBlockNoWhitespace
        ParseDocumentTest("""
            @section Foo {
            @if(true) {
            }}
            """,
            [SectionDirective.Directive]);
    }

    [Fact]
    public void CorrectlyTerminatesWhenCloseBraceImmediatelyFollowsMarkup()
    {
        ParseDocumentTest(
            "@section foo {something}",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void ParsesComment()
    {
        ParseDocumentTest(
            "@section s {<!-- -->}",
            [SectionDirective.Directive]);
    }

    // This was a user reported bug (codeplex #710), the section parser wasn't handling
    // comments.
    [Fact]
    public void ParsesCommentWithDelimiters()
    {
        ParseDocumentTest(
            "@section s {<!-- > \" '-->}",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void CommentRecoversFromUnclosedTag()
    {
        ParseDocumentTest("""
            @section s {
            <a
            <!--  > " '-->}
            """,
            [SectionDirective.Directive]);
    }

    [Fact]
    public void ParsesXmlProcessingInstruction()
    {
        ParseDocumentTest(
            "@section s { <? xml bleh ?>}",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void _WithDoubleTransition1()
    {
        ParseDocumentTest("@section s {<span foo='@@' />}", [SectionDirective.Directive]);
    }

    [Fact]
    public void _WithDoubleTransition2()
    {
        ParseDocumentTest("@section s {<span foo='@DateTime.Now @@' />}", [SectionDirective.Directive]);
    }
}
