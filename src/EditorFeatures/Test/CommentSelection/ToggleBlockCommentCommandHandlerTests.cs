// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.CommentSelection;
using Microsoft.VisualStudio.Commanding;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CommentSelection;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
public class ToggleBlockCommentCommandHandlerTests : AbstractToggleCommentTestBase
{
    [WpfFact]
    public void AddComment_EmptyCaret()
    {
        var markup = @"$$";
        var expected = @"[|/**/|]";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_EmptySelection()
    {
        var markup = @"[| |]";
        var expected = @"[|/* */|]";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_SingleLineSelected()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_SingleLineWithWhitespaceSelected()
    {
        var markup =
@"
class C
{
    void M()
    {
[|        var i = 1;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
[|/*        var i = 1;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CaretInsideSingleLine()
    {
        var markup =
@"
class C
{
    void M()
    {
        var$$ i = 1;
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        var[|/**/|] i = 1;
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_PartialLineSelected()
    {
        var markup =
@"
class C
{
    void M()
    {
        var [|i = 1|];
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        var [|/*i = 1*/|];
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CaretInsideToken()
    {
        var markup =
@"
class C
{
    void M()
    {
        va$$r i = 1;
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        var[|/**/|] i = 1;
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CaretInsideOperatorToken()
    {
        var markup = @"
class C
{
    void M()
    {
        Func<int, bool> myFunc = x =$$> x == 5;
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        Func<int, bool> myFunc = x =>[|/**/|] x == 5;
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CaretInsideNewline()
    {
        var markup =
@"
class C
{
    void M()
    {
        var i = 1;$$
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        var i = 1;[|/**/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_MultiLineSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;
        var k = 3;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        var j = 2;
        var k = 3;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_MultiLineSelectionWithWhitespace()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;
        var k = 3;
    
|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        var j = 2;
        var k = 3;
    
*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_SingleLineCommentInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|//var i = 1;
        var j = 2;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*//var i = 1;
        var j = 2;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_BlockCommentBetweenCodeInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        /*var j = 2;
        var k = 3;*/
        var l = 4;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        *//*var j = 2;
        var k = 3;*//*
        var l = 4;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_SequentialBlockCommentBetweenCodeInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        /*var j = 2;
        var k = 3;*//*
        var l = 4;*/
        var m = 5;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        *//*var j = 2;
        var k = 3;*//*
        var l = 4;*//*
        var m = 5;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_SequentialBlockCommentsAndWhitespaceBetweenCodeInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        /*var j = 2;
        var k = 3;*/

    /*
        var l = 4;*/
        var m = 5;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        *//*var j = 2;
        var k = 3;*/

    /*
        var l = 4;*//*
        var m = 5;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CodeBetweenBlockCommentsInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        var j = 2;*/
        var k = 3;
        /*var l = 4;
        var m = 5;*/|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        var j = 2;*//*
        var k = 3;
        *//*var l = 4;
        var m = 5;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CodeThenCommentInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        /*var j = 2;
        var k = 3;*/|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        *//*var j = 2;
        var k = 3;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CodeThenCommentAndWhitespaceInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        /*var j = 2;
        var k = 3;*/
  |]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        *//*var j = 2;
        var k = 3;*/
  |]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CloseCommentOnlyInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;*/
        var k = 3;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        var j = 2;*/
        var k = 3;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CodeThenPartialCommentInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        /*var j = 2;
        var|] k = 3;*/
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        *//*var j = 2;
        var*/|]/* k = 3;*/
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CommentThenCodeInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        var j = 2;*/
        var k = 3;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        var j = 2;*//*
        var k = 3;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CommentAndWhitespaceThenCodeInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
[|        /*var i = 1;
        var j = 2;*/
        var k = 3;
  |]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
[|        /*var i = 1;
        var j = 2;*//*
        var k = 3;
  */|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CommentCloseMarkerThenCodeInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        /*var i = 1;
        var j = 2;[|*/
        var k = 3;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        /*var i = 1;
        var j = 2;[|*//*
        var k = 3;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CodeThenCommentStartMarkerInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;/*|]
        var k = 3;*/
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        var j = 2;*//*|]
        var k = 3;*/
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_PartialCommentThenCodeInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        /*var i = 1;
        var [|j = 2;*/
        var k = 3;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        /*var i = 1;
        var */[|/*j = 2;*//*
        var k = 3;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CaretBeforeBlockOnNewLine()
    {
        var markup =
@"
class C
{
    void M()
    {$$
        /*var i = 1;*/
    }
}";
        var expected =
@"
class C
{
    void M()
    {[|/**/|]
        /*var i = 1;*/
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CaretBeforeCodeAndBlock()
    {
        var markup =
@"
class C
{
    void M()
    {
    $$    var /*i*/ = 1;
    }
}";
        var expected =
@"
class C
{
    void M()
    {
    [|/**/|]    var /*i*/ = 1;
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CaretAfterBlockOnNewLine()
    {
        var markup =
@"
class C
{
    void M()
    {
        /*var i = 1*/
$$
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        /*var i = 1*/
[|/**/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_CaretAfterBlockAndCode()
    {
        var markup =
@"
class C
{
    void M()
    {
        /*var */i = 1;  $$
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        /*var */i = 1;  [|/**/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_BlockSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;|]
        [|var j = 2;|]
        [|var k = 3;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;*/|]
        [|/*var j = 2;*/|]
        [|/*var k = 3;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_BlockSelectionPartiallyCommented()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|/*var i = 1;|]
        [|var j = 2;*/|]
        [|var k = 3;  |]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;*/|]/*
        */[|/*var j = 2;*/|]
        [|/*var k = 3;  */|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_DirectiveInsideSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
#if false
        var j = 2;
#endif
        var k = 3;|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
#if false
        var j = 2;
#endif
        var k = 3;*/|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void AddComment_WithProjectionBuffer()
    {
        var surfaceMarkup = @"&lt; html &gt;@{|S1:|}";
        var csharpMarkup =
@"
{|S1:class C
{
    void M()
    {
        [|var i = 1;|]
    }
}|}";
        var expected =
@"&lt; html &gt;@class C
{
    void M()
    {
        [|/*var i = 1;*/|]
    }
}";
        ToggleCommentWithProjectionBuffer(surfaceMarkup, csharpMarkup, expected);
    }

    [WpfFact]
    public void RemoveComment_AtBeginningOfFile()
    {
        var markup = @"[|/**/|]";
        var expected = @"";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_CaretInsideBlock()
    {
        var markup =
@"
class C
{
    void M()
    {
        /*var i = 1;
        var $$j = 2;
        var k = 3;*/
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;
        var k = 3;|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_CaretInsideSequentialBlock()
    {
        var markup =
@"
class C
{
    void M()
    {
        /*var i = 1;
        var $$j = 2;*//*
        var k = 3;*/
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;|]/*
        var k = 3;*/
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_CaretBeforeBlockOnlyWhitespace()
    {
        var markup =
@"
class C
{
    void M()
    {
    $$    /*var i = 1;
        var*//* j = 2;*/
        var k = 3;
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var|]/* j = 2;*/
        var k = 3;
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_CaretBeforeMultipleBlocksOnlyWhitespace()
    {
        var markup =
@"
class C
{
    void M()
    {
    $$    /*var*/ i = 1/**/;
        var/* j = 2;*/
        var k = 3;
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var|] i = 1/**/;
        var/* j = 2;*/
        var k = 3;
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_CaretAfterBlockOnlyWhitespace()
    {
        var markup =
@"
class C
{
    void M()
    {
        /*var i = 1;
        var j = 2;*/    $$
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_CaretAfterMultipleBlocksOnlyWhitespace()
    {
        var markup =
@"
class C
{
    void M()
    {
        var i = 1;
        /*var*/ j /*= 2;*/   $$
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        var i = 1;
        /*var*/ j [|= 2;|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_CaretInsideUnclosedBlock()
    {
        var markup =
@"
class C
{
    void M()
    {
        /*var i = 1;
        var $$j = 2;
        var k = 3;
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;
        var k = 3;
    }
}|]";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_CommentInsideSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        var j = 2;
        var k = 3;*/|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;
        var k = 3;|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_CommentAndWhitespaceInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {[|

        /*var i = 1;
        var j = 2;
        var k = 3;*/          |]
    }
}";
        var expected =
@"
class C
{
    void M()
    {

        [|var i = 1;
        var j = 2;
        var k = 3;|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_CommentWithSingleLineCommentInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        //var j = 2;
        var k = 3;*/|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var i = 1;
        //var j = 2;
        var k = 3;|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_SequentialBlockInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        *//*var j = 2;
        var k = 3;*/|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;
        var k = 3;|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_SequentialBlockAndWhitespaceInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {[|

        /*var i = 1;
        */   
  /*var j = 2;
        var k = 3;*/       |]
    }
}";
        var expected =
@"
class C
{
    void M()
    {

        [|var i = 1;

        var j = 2;
        var k = 3;|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_CommentPartiallyInsideSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        /*var [|i = 1;
        var j = 2;|]
        var k = 3;*/
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;
        var k = 3;|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_PartialSequentialBlockInSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        /*var [|i = 1;
        *//*var j = 2;
        var |]k = 3;*/
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;
        var k = 3;|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_BlockSelectionWithMultipleComments()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|/*var i = 1;*/|]
        [|/*var j = 2;*/|]
        [|/*var k = 3;*/|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var i = 1;|]
        [|var j = 2;|]
        [|var k = 3;|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_BlockSelectionWithOneComment()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|/*var i = 1;|]
        [|var j = 2;  |]
        [|var k = 3;*/|]
    }
}";
        var expected =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;
        var k = 3;|]
    }
}";

        ToggleComment(markup, expected);
    }

    [WpfFact]
    public void RemoveComment_WithProjectionBuffer()
    {
        var surfaceMarkup = @"&lt; html &gt;@{|S1:|}";
        var csharpMarkup =
@"
{|S1:class C
{
    void M()
    {
        [|/*var i = 1;*/|]
    }
}|}";
        var expected =
@"&lt; html &gt;@class C
{
    void M()
    {
        [|var i = 1;|]
    }
}";
        ToggleCommentWithProjectionBuffer(surfaceMarkup, csharpMarkup, expected);
    }

    [WpfFact]
    public void ToggleComment_MultiLineSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;
        var k = 3;|]
    }
}";
        var expectedText = new[]
        {
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        var j = 2;
        var k = 3;*/|]
    }
}",
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;
        var k = 3;|]
    }
}"
        };

        ToggleCommentMultiple(markup, expectedText);
    }

    [WpfFact]
    public void ToggleComment_MultiCommentSelection()
    {
        var markup =
@"
class C
{
    void M()
    {
        /*var i = 1;
        [|var */ j = 2;
        var k = 3;|]
    }
}";
        var expectedText = new[]
        {
@"
class C
{
    void M()
    {
        /*var i = 1;
        */[|/*var *//* j = 2;
        var k = 3;*/|]
    }
}",
@"
class C
{
    void M()
    {
        /*var i = 1;
        */
        [|var j = 2;
        var k = 3;|]
    }
}"
        };

        ToggleCommentMultiple(markup, expectedText);
    }

    internal override AbstractCommentSelectionBase<ValueTuple> GetToggleCommentCommandHandler(EditorTestWorkspace workspace)
    {
        return (AbstractCommentSelectionBase<ValueTuple>)workspace.ExportProvider.GetExportedValues<ICommandHandler>()
            .First(export => typeof(ToggleBlockCommentCommandHandler).Equals(export.GetType()));
    }

    internal override EditorTestWorkspace GetWorkspace(string markup, TestComposition composition)
        => EditorTestWorkspace.CreateCSharp(markup, composition: composition);
}
