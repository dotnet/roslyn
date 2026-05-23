// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpErrorTest() : ParserTestBase(layer: TestProject.Layer.Compiler)
{
    [Fact]
    public void HandlesQuotesAfterTransition()
    {
        ParseDocumentTest("@\"");
    }

    [Fact]
    public void WithHelperDirectiveProducesError()
    {
        ParseDocumentTest("@helper fooHelper { }");
    }

    [Fact]
    public void WithNestedCodeBlockProducesError()
    {
        ParseDocumentTest("@if { @{} }");
    }

    [Fact]
    public void CapturesWhitespaceToEOLInInvalidUsingStmtAndTreatsAsFileCode()
    {
        // ParseBlockCapturesWhitespaceToEndOfLineInInvalidUsingStatementAndTreatsAsFileCode
        ParseDocumentTest("""
            @using          


            """);
    }

    [Fact]
    public void MethodOutputsOpenCurlyAsCodeSpanIfEofFoundAfterOpenCurlyBrace()
    {
        ParseDocumentTest("@{");
    }

    [Fact]
    public void MethodOutputsZeroLengthCodeSpanIfStatementBlockEmpty()
    {
        ParseDocumentTest("@{}");
    }

    [Fact]
    public void MethodProducesErrorIfNewlineFollowsTransition()
    {
        ParseDocumentTest("""
            @

            """);
    }

    [Fact]
    public void MethodProducesErrorIfWhitespaceBetweenTransitionAndBlockStartInEmbeddedExpr()
    {
        // ParseBlockMethodProducesErrorIfWhitespaceBetweenTransitionAndBlockStartInEmbeddedExpression
        ParseDocumentTest("""
            @{
                @   {}
            }
            """);
    }

    [Fact]
    public void MethodProducesErrorIfEOFAfterTransitionInEmbeddedExpression()
    {
        ParseDocumentTest("""
            @{
                @
            """);
    }

    [Fact]
    public void MethodParsesNothingIfFirstCharacterIsNotIdentifierStartOrParenOrBrace()
    {
        ParseDocumentTest("@!!!");
    }

    [Fact]
    public void ShouldReportErrorAndTerminateAtEOFIfIfParenInExplicitExprUnclosed()
    {
        // ParseBlockShouldReportErrorAndTerminateAtEOFIfIfParenInExplicitExpressionUnclosed
        ParseDocumentTest("""
            @(foo bar
            baz
            """);
    }

    [Fact]
    public void ShouldReportErrorAndTerminateAtMarkupIfIfParenInExplicitExprUnclosed()
    {
        // ParseBlockShouldReportErrorAndTerminateAtMarkupIfIfParenInExplicitExpressionUnclosed
        ParseDocumentTest("""
            @(foo bar
            <html>
            baz
            </html
            """);
    }

    [Fact]
    public void CorrectlyHandlesInCorrectTransitionsIfImplicitExpressionParensUnclosed()
    {
        ParseDocumentTest("""
            @Href(
            <h1>@Html.Foo(Bar);</h1>

            """);
    }

    [Fact]
    // Test for fix to Dev10 884975 - Incorrect Error Messaging
    public void ShouldReportErrorAndTerminateAtEOFIfParenInImplicitExprUnclosed()
    {
        // ParseBlockShouldReportErrorAndTerminateAtEOFIfParenInImplicitExpressionUnclosed
        ParseDocumentTest("""
            @Foo(Bar(Baz)
            Biz
            Boz
            """);
    }

    [Fact]
    // Test for fix to Dev10 884975 - Incorrect Error Messaging
    public void ShouldReportErrorAndTerminateAtMarkupIfParenInImplicitExpressionUnclosed()
    {
        // ParseBlockShouldReportErrorAndTerminateAtMarkupIfParenInImplicitExpressionUnclosed
        ParseDocumentTest("""
            @Foo(Bar(Baz)
            Biz
            <html>
            Boz
            </html>
            """);
    }

    [Fact]
    // Test for fix to Dev10 884975 - Incorrect Error Messaging
    public void ShouldReportErrorAndTerminateAtEOFIfBracketInImplicitExpressionUnclosed()
    {
        // ParseBlockShouldReportErrorAndTerminateAtEOFIfBracketInImplicitExpressionUnclosed
        ParseDocumentTest("""
            @Foo[Bar[Baz]
            Biz
            Boz
            """);
    }

    [Fact]
    // Test for fix to Dev10 884975 - Incorrect Error Messaging
    public void ShouldReportErrorAndTerminateAtMarkupIfBracketInImplicitExprUnclosed()
    {
        // ParseBlockShouldReportErrorAndTerminateAtMarkupIfBracketInImplicitExpressionUnclosed
        ParseDocumentTest("""
            @Foo[Bar[Baz]
            Biz
            <b>
            Boz
            </b>
            """);
    }

    // Simple EOF handling errors:
    [Fact]
    public void ReportsErrorIfExplicitCodeBlockUnterminatedAtEOF()
    {
        ParseDocumentTest("@{ var foo = bar; if(foo != null) { bar(); } ");
    }

    [Fact]
    public void ReportsErrorIfClassBlockUnterminatedAtEOF()
    {
        ParseDocumentTest(
            "@functions { var foo = bar; if(foo != null) { bar(); } ",
            [FunctionsDirective.Directive]);
    }

    [Fact]
    public void ReportsErrorIfIfBlockUnterminatedAtEOF()
    {
        RunUnterminatedSimpleKeywordBlock("@if");
    }

    [Fact]
    public void ReportsErrorIfElseBlockUnterminatedAtEOF()
    {
        ParseDocumentTest("@if(foo) { baz(); } else { var foo = bar; if(foo != null) { bar(); } ");
    }

    [Fact]
    public void ReportsErrorIfElseIfBlockUnterminatedAtEOF()
    {
        ParseDocumentTest("@if(foo) { baz(); } else if { var foo = bar; if(foo != null) { bar(); } ");
    }

    [Fact]
    public void ReportsErrorIfDoBlockUnterminatedAtEOF()
    {
        ParseDocumentTest("@do { var foo = bar; if(foo != null) { bar(); } ");
    }

    [Fact]
    public void ReportsErrorIfTryBlockUnterminatedAtEOF()
    {
        ParseDocumentTest("@try { var foo = bar; if(foo != null) { bar(); } ");
    }

    [Fact]
    public void ReportsErrorIfCatchBlockUnterminatedAtEOF()
    {
        ParseDocumentTest("@try { baz(); } catch(Foo) { var foo = bar; if(foo != null) { bar(); } ");
    }

    [Fact]
    public void ReportsErrorIfFinallyBlockUnterminatedAtEOF()
    {
        ParseDocumentTest("@try { baz(); } finally { var foo = bar; if(foo != null) { bar(); } ");
    }

    [Fact]
    public void ReportsErrorIfForBlockUnterminatedAtEOF()
    {
        RunUnterminatedSimpleKeywordBlock("@for");
    }

    [Fact]
    public void ReportsErrorIfForeachBlockUnterminatedAtEOF()
    {
        RunUnterminatedSimpleKeywordBlock("@foreach");
    }

    [Fact]
    public void ReportsErrorIfWhileBlockUnterminatedAtEOF()
    {
        RunUnterminatedSimpleKeywordBlock("@while");
    }

    [Fact]
    public void ReportsErrorIfSwitchBlockUnterminatedAtEOF()
    {
        RunUnterminatedSimpleKeywordBlock("@switch");
    }

    [Fact]
    public void ReportsErrorIfLockBlockUnterminatedAtEOF()
    {
        RunUnterminatedSimpleKeywordBlock("@lock");
    }

    [Fact]
    public void ReportsErrorIfUsingBlockUnterminatedAtEOF()
    {
        RunUnterminatedSimpleKeywordBlock("@using");
    }

    [Fact]
    public void RequiresControlFlowStatementsToHaveBraces()
    {
        ParseDocumentTest("@if(foo) <p>Bar</p> else if(bar) <p>Baz</p> else <p>Boz</p>");
    }

    [Fact]
    public void IncludesUnexpectedCharacterInSingleStatementControlFlowStatementError()
    {
        ParseDocumentTest("@if(foo)) { var bar = foo; }");
    }

    [Fact]
    public void OutputsErrorIfAtSignFollowedByLessThanSignAtStatementStart()
    {
        ParseDocumentTest("@if(foo) { @<p>Bar</p> }");
    }

    [Fact]
    public void TerminatesIfBlockAtEOLWhenRecoveringFromMissingCloseParen()
    {
        ParseDocumentTest("""
            @if(foo bar
            baz
            """);
    }

    [Fact]
    public void TerminatesForeachBlockAtEOLWhenRecoveringFromMissingCloseParen()
    {
        ParseDocumentTest("""
            @foreach(foo bar
            baz
            """);
    }

    [Fact]
    public void TerminatesWhileClauseInDoStmtAtEOLWhenRecoveringFromMissingCloseParen()
    {
        ParseDocumentTest("""
            @do { } while(foo bar
            baz
            """);
    }

    [Fact]
    public void TerminatesUsingBlockAtEOLWhenRecoveringFromMissingCloseParen()
    {
        ParseDocumentTest("""
            @using(foo bar
            baz
            """);
    }

    [Fact]
    public void ResumesIfStatementAfterOpenParen()
    {
        ParseDocumentTest("""
            @if(
            else { <p>Foo</p> }
            """);
    }

    [Fact]
    public void TerminatesNormalCSharpStringsAtEOLIfEndQuoteMissing()
    {
        ParseDocumentTest("""
            @if(foo) {
                var p = "foo bar baz
            ;
            }
            """);
    }

    [Fact]
    public void TerminatesNormalStringAtEndOfFile()
    {
        ParseDocumentTest("@if(foo) { var foo = \"blah blah blah blah blah");
    }

    [Fact]
    public void TerminatesVerbatimStringAtEndOfFile()
    {
        ParseDocumentTest("""
            @if(foo) { var foo = @"blah 
            blah; 
            <p>Foo</p>
            blah 
            blah
            """);
    }

    [Fact]
    public void CorrectlyParsesMarkupIncorrectyAssumedToBeWithinAStatement()
    {
        ParseDocumentTest("""
            @if(foo) {
                var foo = "foo bar baz
                <p>Foo is @foo</p>
            }
            """);
    }

    [Fact]
    public void CorrectlyParsesAtSignInDelimitedBlock()
    {
        ParseDocumentTest("@(Request[\"description\"] ?? @photo.Description)");
    }

    [Fact]
    public void CorrectlyRecoversFromMissingCloseParenInExpressionWithinCode()
    {
        ParseDocumentTest("@{string.Format(<html></html>}");
    }

    private void RunUnterminatedSimpleKeywordBlock(string keyword)
    {
        ParseDocumentTest(
            keyword + " (foo) { var foo = bar; if(foo != null) { bar(); } ");
    }
}
