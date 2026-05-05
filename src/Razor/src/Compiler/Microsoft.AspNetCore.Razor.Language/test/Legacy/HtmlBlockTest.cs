// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class HtmlBlockTest() : ParserTestBase(layer: TestProject.Layer.Compiler)
{
    [Fact]
    public void HandlesUnbalancedTripleDashHTMLComments()
    {
        ParseDocumentTest(
@"@{
    <!-- Hello, I'm a comment that shouldn't break razor --->
}");
    }

    [Fact]
    public void HandlesOpenAngleAtEof()
    {
        ParseDocumentTest("""
            @{
            <
            """);
    }

    [Fact]
    public void HandlesOpenAngleWithProperTagFollowingIt()
    {
        ParseDocumentTest("""
            @{
            <
            </html>
            """,
                        designTime: true);
    }

    [Fact]
    public void TagWithoutCloseAngleDoesNotTerminateBlock()
    {
        ParseDocumentTest("""
            @{<                      
               
            """);
    }

    [Fact]
    public void AllowsStartAndEndTagsToDifferInCase()
    {
        ParseDocumentTest("@{<li><p>Foo</P></lI>}");
    }

    [Fact]
    public void ReadsToEndOfLineIfFirstCharacterAfterTransitionIsColon()
    {
        ParseDocumentTest("""
            @{@:<li>Foo Bar Baz
            bork}
            """);
    }

    [Fact]
    public void StopsParsingSingleLineBlockAtEOFIfNoEOLReached()
    {
        ParseDocumentTest("@{@:foo bar");
    }

    [Fact]
    public void StopsAtMatchingCloseTagToStartTag()
    {
        ParseDocumentTest("@{<a><b></b></a><c></c>}");
    }

    [Fact]
    public void ParsesUntilMatchingEndTagIfFirstNonWhitespaceCharacterIsStartTag()
    {
        ParseDocumentTest("@{<baz><boz><biz></biz></boz></baz>}");
    }

    [Fact]
    public void AllowsUnclosedTagsAsLongAsItCanRecoverToAnExpectedEndTag()
    {
        ParseDocumentTest("@{<foo><bar><baz></foo>}");
    }

    [Fact]
    public void WithSelfClosingTagJustEmitsTag()
    {
        ParseDocumentTest("@{<foo />}");
    }

    [Fact]
    public void CanHandleSelfClosingTagsWithinBlock()
    {
        ParseDocumentTest("@{<foo><bar /></foo>}");
    }

    [Fact]
    public void SupportsTagsWithAttributes()
    {
        ParseDocumentTest("@{<foo bar=\"baz\"><biz><boz zoop=zork/></biz></foo>}");
    }

    [Fact]
    public void AllowsCloseAngleBracketInAttributeValueIfDoubleQuoted()
    {
        ParseDocumentTest("@{<foo><bar baz=\">\" /></foo>}");
    }

    [Fact]
    public void AllowsCloseAngleBracketInAttributeValueIfSingleQuoted()
    {
        ParseDocumentTest("@{<foo><bar baz=\'>\' /></foo>}");
    }

    [Fact]
    public void AllowsSlashInAttributeValueIfDoubleQuoted()
    {
        ParseDocumentTest("@{<foo><bar baz=\"/\"></bar></foo>}");
    }

    [Fact]
    public void AllowsSlashInAttributeValueIfSingleQuoted()
    {
        ParseDocumentTest("@{<foo><bar baz=\'/\'></bar></foo>}");
    }

    [Fact]
    public void TerminatesAtEOF()
    {
        ParseDocumentTest("@{<foo>");
    }

    [Fact]
    public void SupportsCommentAsBlock()
    {
        ParseDocumentTest("@{<!-- foo -->}");
    }

    [Fact]
    public void SupportsCommentWithExtraDashAsBlock()
    {
        ParseDocumentTest("@{<!-- foo --->}");
    }

    [Fact]
    public void SupportsCommentWithinBlock()
    {
        ParseDocumentTest("@{<foo>bar<!-- zoop -->baz</foo>}");
    }

    [Fact]
    public void HtmlCommentSupportsMultipleDashes()
    {
        ParseDocumentTest(
@"<div><!--- Hello World ---></div>
<div><!---- Hello World ----></div>
<div><!----- Hello World -----></div>
<div><!----- Hello < --- > World </div> -----></div>
");
    }

    [Fact]
    public void ProperlyBalancesCommentStartAndEndTags()
    {
        ParseDocumentTest("@{<!--<foo></bar>-->}");
    }

    [Fact]
    public void TerminatesAtEOFWhenParsingComment()
    {
        ParseDocumentTest("@{<!--<foo>");
    }

    [Fact]
    public void OnlyTerminatesCommentOnFullEndSequence()
    {
        ParseDocumentTest("@{<!--<foo>--</bar>-->}");
    }

    [Fact]
    public void TerminatesCommentAtFirstOccurrenceOfEndSequence()
    {
        ParseDocumentTest("@{<foo><!--<foo></bar-->--></foo>}");
    }

    [Fact]
    public void TreatsMalformedTagsAsContent()
    {
        ParseDocumentTest("@{<foo></!-- bar --></foo>}");
    }


    [Fact]
    public void ParsesSGMLDeclarationAsEmptyTag()
    {
        ParseDocumentTest("@{<foo><!DOCTYPE foo bar baz></foo>}");
    }

    [Fact]
    public void TerminatesSGMLDeclarationAtFirstCloseAngle()
    {
        ParseDocumentTest("@{<foo><!DOCTYPE foo bar> baz></foo>}");
    }

    [Fact]
    public void ParsesXMLProcessingInstructionAsEmptyTag()
    {
        ParseDocumentTest("@{<foo><?xml foo bar baz?></foo>}");
    }

    [Fact]
    public void TerminatesXMLProcessingInstructionAtQuestionMarkCloseAnglePair()
    {
        ParseDocumentTest("@{<foo><?xml foo bar baz?> baz</foo>}");
    }

    [Fact]
    public void DoesNotTerminateXMLProcInstrAtCloseAngleUnlessPreceededByQuestionMark()
    {
        // ParseBlockDoesNotTerminateXMLProcessingInstructionAtCloseAngleUnlessPreceededByQuestionMark
        ParseDocumentTest("@{<foo><?xml foo bar> baz?></foo>}");
    }

    [Fact]
    public void SupportsScriptTagsWithLessThanSignsInThem()
    {
        ParseDocumentTest(@"@{<script>if(foo<bar) { alert(""baz"");)</script>}");
    }

    [Fact]
    public void SupportsScriptTagsWithSpacedLessThanSignsInThem()
    {
        ParseDocumentTest(@"@{<script>if(foo < bar) { alert(""baz"");)</script>}");
    }

    [Fact]
    public void AcceptsEmptyTextTag()
    {
        ParseDocumentTest("@{<text/>}");
    }

    [Fact]
    public void AcceptsTextTagAsOuterTagButDoesNotRender()
    {
        ParseDocumentTest("@{<text>Foo Bar <foo> Baz</text> zoop}");
    }

    [Fact]
    public void RendersLiteralTextTagIfDoubled()
    {
        ParseDocumentTest("@{<text><text>Foo Bar <foo> Baz</text></text> zoop}");
    }

    [Fact]
    public void DoesNotConsiderPsuedoTagWithinMarkupBlock()
    {
        ParseDocumentTest("@{<foo><text><bar></bar></foo>}");
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10180")]
    public void TextAfterCodeBlockInMarkupTransition_01()
    {
        ParseDocumentTest("""
            @{
                @:@{ <i>x y z </i> }
                <text>a b c</text>
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10180")]
    public void TextAfterCodeBlockInMarkupTransition_02()
    {
        ParseDocumentTest("""
            @{
                @:@{ <i>x y z </i> }
                <text><b>a b c</b></text>
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10180")]
    public void TextAfterCodeBlockInMarkupTransition_03()
    {
        ParseDocumentTest("""
            @{
                @:@{ <i>x y z </i> }
                <b>a b c</b>
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10180")]
    public void TextAfterCodeBlockInMarkupTransition_04()
    {
        ParseDocumentTest("""
            @{
                @:@{
                    <i>x
                    y
                    z </i>
                }
                <text>a
                b
                c</text>
            }
            """);
    }

    [Fact]
    public void StopsParsingMidEmptyTagIfEOFReached()
    {
        ParseDocumentTest("@{<br/}");
    }

    [Fact]
    public void CorrectlyHandlesSingleLineOfMarkupWithEmbeddedStatement()
    {
        ParseDocumentTest("@{<div>Foo @if(true) {} Bar</div>}");
    }

    [Fact]
    public void IgnoresTagsInContentsOfScriptTag()
    {
        ParseDocumentTest(@"@{<script>foo<bar baz='@boz'></script>}");
    }

    [Fact]
    public void HandlesForwardSlashInAttributeContent()
    {
        ParseDocumentTest(@"@{<p / class=foo />}");
    }
}
