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
public sealed class ToggleBlockCommentCommandHandlerTests : AbstractToggleCommentTestBase
{
    [WpfFact]
    public void AddComment_EmptyCaret()
        => ToggleComment(@"$$", @"[|/**/|]");

    [WpfFact]
    public void AddComment_EmptySelection()
        => ToggleComment(@"[| |]", @"[|/* */|]");

    [WpfFact]
    public void AddComment_SingleLineSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_SingleLineWithWhitespaceSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
            [|        var i = 1;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
            [|/*        var i = 1;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_CaretInsideSingleLine()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    var$$ i = 1;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var[|/**/|] i = 1;
                }
            }
            """);

    [WpfFact]
    public void AddComment_PartialLineSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    var [|i = 1|];
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var [|/*i = 1*/|];
                }
            }
            """);

    [WpfFact]
    public void AddComment_CaretInsideToken()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    va$$r i = 1;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var[|/**/|] i = 1;
                }
            }
            """);

    [WpfFact]
    public void AddComment_CaretInsideOperatorToken()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    Func<int, bool> myFunc = x =$$> x == 5;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    Func<int, bool> myFunc = x =>[|/**/|] x == 5;
                }
            }
            """);

    [WpfFact]
    public void AddComment_CaretInsideNewline()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    var i = 1;$$
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var i = 1;[|/**/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_MultiLineSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;
                    var k = 3;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    var j = 2;
                    var k = 3;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_MultiLineSelectionWithWhitespace()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;
                    var k = 3;

            |]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    var j = 2;
                    var k = 3;

            */|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_SingleLineCommentInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|//var i = 1;
                    var j = 2;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*//var i = 1;
                    var j = 2;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_BlockCommentBetweenCodeInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    /*var j = 2;
                    var k = 3;*/
                    var l = 4;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    *//*var j = 2;
                    var k = 3;*//*
                    var l = 4;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_SequentialBlockCommentBetweenCodeInSelection()
        => ToggleComment("""
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
            }
            """, """
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
            }
            """);

    [WpfFact]
    public void AddComment_SequentialBlockCommentsAndWhitespaceBetweenCodeInSelection()
        => ToggleComment("""
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
            }
            """, """
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
            }
            """);

    [WpfFact]
    public void AddComment_CodeBetweenBlockCommentsInSelection()
        => ToggleComment("""
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
            }
            """, """
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
            }
            """);

    [WpfFact]
    public void AddComment_CodeThenCommentInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    /*var j = 2;
                    var k = 3;*/|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    *//*var j = 2;
                    var k = 3;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_CodeThenCommentAndWhitespaceInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    /*var j = 2;
                    var k = 3;*/
              |]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    *//*var j = 2;
                    var k = 3;*/
              |]
                }
            }
            """);

    [WpfFact]
    public void AddComment_CloseCommentOnlyInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;*/
                    var k = 3;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    var j = 2;*/
                    var k = 3;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_CodeThenPartialCommentInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    /*var j = 2;
                    var|] k = 3;*/
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    *//*var j = 2;
                    var*/|]/* k = 3;*/
                }
            }
            """);

    [WpfFact]
    public void AddComment_CommentThenCodeInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    var j = 2;*/
                    var k = 3;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    var j = 2;*//*
                    var k = 3;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_CommentAndWhitespaceThenCodeInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
            [|        /*var i = 1;
                    var j = 2;*/
                    var k = 3;
              |]
                }
            }
            """, """
            class C
            {
                void M()
                {
            [|        /*var i = 1;
                    var j = 2;*//*
                    var k = 3;
              */|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_CommentCloseMarkerThenCodeInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    /*var i = 1;
                    var j = 2;[|*/
                    var k = 3;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    /*var i = 1;
                    var j = 2;[|*//*
                    var k = 3;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_CodeThenCommentStartMarkerInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;/*|]
                    var k = 3;*/
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    var j = 2;*//*|]
                    var k = 3;*/
                }
            }
            """);

    [WpfFact]
    public void AddComment_PartialCommentThenCodeInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    /*var i = 1;
                    var [|j = 2;*/
                    var k = 3;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    /*var i = 1;
                    var */[|/*j = 2;*//*
                    var k = 3;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_CaretBeforeBlockOnNewLine()
        => ToggleComment("""
            class C
            {
                void M()
                {$$
                    /*var i = 1;*/
                }
            }
            """, """
            class C
            {
                void M()
                {[|/**/|]
                    /*var i = 1;*/
                }
            }
            """);

    [WpfFact]
    public void AddComment_CaretBeforeCodeAndBlock()
        => ToggleComment("""
            class C
            {
                void M()
                {
                $$    var /*i*/ = 1;
                }
            }
            """, """
            class C
            {
                void M()
                {
                [|/**/|]    var /*i*/ = 1;
                }
            }
            """);

    [WpfFact]
    public void AddComment_CaretAfterBlockOnNewLine()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    /*var i = 1*/
            $$
                }
            }
            """, """
            class C
            {
                void M()
                {
                    /*var i = 1*/
            [|/**/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_CaretAfterBlockAndCode()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    /*var */i = 1;  $$
                }
            }
            """, """
            class C
            {
                void M()
                {
                    /*var */i = 1;  [|/**/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_BlockSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;|]
                    [|var j = 2;|]
                    [|var k = 3;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;*/|]
                    [|/*var j = 2;*/|]
                    [|/*var k = 3;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_BlockSelectionPartiallyCommented()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|/*var i = 1;|]
                    [|var j = 2;*/|]
                    [|var k = 3;  |]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;*/|]/*
                    */[|/*var j = 2;*/|]
                    [|/*var k = 3;  */|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_DirectiveInsideSelection()
        => ToggleComment("""
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
            }
            """, """
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
            }
            """);

    [WpfFact]
    public void AddComment_WithProjectionBuffer()
        => ToggleCommentWithProjectionBuffer(@"&lt; html &gt;@{|S1:|}", """
            {|S1:class C
            {
                void M()
                {
                    [|var i = 1;|]
                }
            }|}
            """, """
            &lt; html &gt;@class C
            {
                void M()
                {
                    [|/*var i = 1;*/|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_AtBeginningOfFile()
        => ToggleComment(@"[|/**/|]", @"");

    [WpfFact]
    public void RemoveComment_CaretInsideBlock()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    /*var i = 1;
                    var $$j = 2;
                    var k = 3;*/
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;
                    var k = 3;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CaretInsideSequentialBlock()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    /*var i = 1;
                    var $$j = 2;*//*
                    var k = 3;*/
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;|]/*
                    var k = 3;*/
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CaretBeforeBlockOnlyWhitespace()
        => ToggleComment("""
            class C
            {
                void M()
                {
                $$    /*var i = 1;
                    var*//* j = 2;*/
                    var k = 3;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var|]/* j = 2;*/
                    var k = 3;
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CaretBeforeMultipleBlocksOnlyWhitespace()
        => ToggleComment("""
            class C
            {
                void M()
                {
                $$    /*var*/ i = 1/**/;
                    var/* j = 2;*/
                    var k = 3;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var|] i = 1/**/;
                    var/* j = 2;*/
                    var k = 3;
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CaretAfterBlockOnlyWhitespace()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    /*var i = 1;
                    var j = 2;*/    $$
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CaretAfterMultipleBlocksOnlyWhitespace()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    var i = 1;
                    /*var*/ j /*= 2;*/   $$
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var i = 1;
                    /*var*/ j [|= 2;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CaretInsideUnclosedBlock()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    /*var i = 1;
                    var $$j = 2;
                    var k = 3;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;
                    var k = 3;
                }
            }|]
            """);

    [WpfFact]
    public void RemoveComment_CommentInsideSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    var j = 2;
                    var k = 3;*/|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;
                    var k = 3;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CommentAndWhitespaceInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {[|

                    /*var i = 1;
                    var j = 2;
                    var k = 3;*/          |]
                }
            }
            """, """
            class C
            {
                void M()
                {

                    [|var i = 1;
                    var j = 2;
                    var k = 3;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CommentWithSingleLineCommentInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    //var j = 2;
                    var k = 3;*/|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;
                    //var j = 2;
                    var k = 3;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_SequentialBlockInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    *//*var j = 2;
                    var k = 3;*/|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;
                    var k = 3;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_SequentialBlockAndWhitespaceInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {[|

                    /*var i = 1;
                    */   
              /*var j = 2;
                    var k = 3;*/       |]
                }
            }
            """, """
            class C
            {
                void M()
                {

                    [|var i = 1;

                    var j = 2;
                    var k = 3;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CommentPartiallyInsideSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    /*var [|i = 1;
                    var j = 2;|]
                    var k = 3;*/
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;
                    var k = 3;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_PartialSequentialBlockInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    /*var [|i = 1;
                    *//*var j = 2;
                    var |]k = 3;*/
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;
                    var k = 3;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_BlockSelectionWithMultipleComments()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|/*var i = 1;*/|]
                    [|/*var j = 2;*/|]
                    [|/*var k = 3;*/|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;|]
                    [|var j = 2;|]
                    [|var k = 3;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_BlockSelectionWithOneComment()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|/*var i = 1;|]
                    [|var j = 2;  |]
                    [|var k = 3;*/|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;
                    var k = 3;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_WithProjectionBuffer()
        => ToggleCommentWithProjectionBuffer(@"&lt; html &gt;@{|S1:|}", """
            {|S1:class C
            {
                void M()
                {
                    [|/*var i = 1;*/|]
                }
            }|}
            """, """
            &lt; html &gt;@class C
            {
                void M()
                {
                    [|var i = 1;|]
                }
            }
            """);

    [WpfFact]
    public void ToggleComment_MultiLineSelection()
    {
        var expectedText = new[]
        {
            """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    var j = 2;
                    var k = 3;*/|]
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;
                    var k = 3;|]
                }
            }
            """
        };

        ToggleCommentMultiple("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;
                    var k = 3;|]
                }
            }
            """, expectedText);
    }

    [WpfFact]
    public void ToggleComment_MultiCommentSelection()
    {
        var expectedText = new[]
        {
            """
            class C
            {
                void M()
                {
                    /*var i = 1;
                    */[|/*var *//* j = 2;
                    var k = 3;*/|]
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    /*var i = 1;
                    */
                    [|var j = 2;
                    var k = 3;|]
                }
            }
            """
        };

        ToggleCommentMultiple("""
            class C
            {
                void M()
                {
                    /*var i = 1;
                    [|var */ j = 2;
                    var k = 3;|]
                }
            }
            """, expectedText);
    }

    internal override AbstractCommentSelectionBase<ValueTuple> GetToggleCommentCommandHandler(EditorTestWorkspace workspace)
    {
        return (AbstractCommentSelectionBase<ValueTuple>)workspace.ExportProvider.GetExportedValues<ICommandHandler>()
            .First(export => typeof(ToggleBlockCommentCommandHandler).Equals(export.GetType()));
    }

    internal override EditorTestWorkspace GetWorkspace(string markup, TestComposition composition)
        => EditorTestWorkspace.CreateCSharp(markup, composition: composition);
}
