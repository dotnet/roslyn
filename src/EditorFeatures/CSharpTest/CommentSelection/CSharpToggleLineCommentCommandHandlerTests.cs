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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CommentSelection;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.ToggleLineComment)]
public sealed class CSharpToggleLineCommentCommandHandlerTests : AbstractToggleCommentTestBase
{
    [WpfFact]
    public void AddComment_EmptyCaret()
        => ToggleComment(@"$$", @"[||]");

    [WpfFact]
    public void AddComment_EmptySelection()
        => ToggleComment(@"[| |]", @"[||]");

    [WpfFact]
    public void AddComment_CaretInUncommentedLine()
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
                    //var[||] i = 1;
                }
            }
            """);

    [WpfFact]
    public void AddComment_CaretBeforeUncommentedLine()
        => ToggleComment("""
            class C
            {
                void M()
                {
            $$        var i = 1;
                }
            }
            """, """
            class C
            {
                void M()
                {
            [||]        //var i = 1;
                }
            }
            """);

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
                    [|//var i = 1;|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_PartialSingleLineSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    var [|i = 1;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    //var [|i = 1;|]
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
            [|
                    var i = 1;
               |]
                }
            }
            """, """
            class C
            {
                void M()
                {
            [|
                    //var i = 1;
               |]
                }
            }
            """);

    [WpfFact]
    public void AddComment_SelectionInsideCommentAtEndOfLine()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    var i = 1; // A [|comment|].
                }
            }
            """, """
            class C
            {
                void M()
                {
                    //var i = 1; // A [|comment|].
                }
            }
            """);

    [WpfFact]
    public void AddComment_SelectionAroundCommentAtEndOfLine()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    var i = 1; [|// A comment.|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    //var i = 1; [|// A comment.|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_SelectionOutsideCommentAtEndOfLine()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1; // A comment.|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|//var i = 1; // A comment.|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_CaretOutsideCommentAtEndOfLine()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    var $$i = 1; // A comment.
                }
            }
            """, """
            class C
            {
                void M()
                {
                    //var [||]i = 1; // A comment.
                }
            }
            """);

    [WpfFact]
    public void AddComment_CaretInsideCommentAtEndOfLine()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    var i = 1; // A $$comment.
                }
            }
            """, """
            class C
            {
                void M()
                {
                    //var i = 1; // A [||]comment.
                }
            }
            """);

    [WpfFact]
    public void AddComment_CommentMarkerInString()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|string s = '\\';|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|//string s = '\\';|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_MultipleLinesSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|//var i = 1;
                    //var j = 2;|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_MultipleLinesWithWhitespaceSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
            [|
                    var i = 1;

                    var j = 2;
               |]
                }
            }
            """, """
            class C
            {
                void M()
                {
            [|
                    //var i = 1;

                    //var j = 2;
               |]
                }
            }
            """);

    [WpfFact]
    public void AddComment_MultipleLinesPartiallyCommentedSelected()
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
                    [|////var i = 1;
                    //var j = 2;|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_MultipleLinesWithCommentsInLineSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1; // A comment.
                    var j = 2;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|//var i = 1; // A comment.
                    //var j = 2;|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_MultipleLinesWithDifferentIndentationsSelected()
        => ToggleComment("""
            class C
            {
            [|    void M()
                {
                    var i = 1;

                    var j = 2;
                }|]
            }
            """, """
            class C
            {
            [|    //void M()
                //{
                //    var i = 1;

                //    var j = 2;
                //}|]
            }
            """);

    [WpfFact]
    public void AddComment_MultiCaret()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    var [||]i = 1;
                    var [||]j = 2;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    //var [||]i = 1;
                    //var [||]j = 2;
                }
            }
            """);

    [WpfFact]
    public void AddComment_MultiSeletion()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;|]
                    [|var j = 2;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|//var i = 1;|]
                    [|//var j = 2;|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_MultiSeletionPartiallyCommented()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|//var i = |]1;
                    [|var j = 2;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|////var i = |]1;
                    [|//var j = 2;|]
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
                    [|//var i = 1;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CaretInCommentedLine()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    //var$$ i = 1;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var[||] i = 1;
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CaretBeforeCommentedLine()
        => ToggleComment("""
            class C
            {
                void M()
                {
                $$    //var i = 1;
                }
            }
            """, """
            class C
            {
                void M()
                {
                [||]    var i = 1;
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CaretInCommentedLineWithEndComment()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    //var i = 1; // A $$comment.
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var i = 1; // A [||]comment.
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CaretInDoubleCommentedLine()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    ////var$$ i = 1;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    //var[||] i = 1;
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CommentedLineSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|//var i = 1;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_InsideCommentSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    //var [|i = 1;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var [|i = 1;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CommentedLineWithWhitespaceSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
            [|
                    //var i = 1;
              |]
                }
            }
            """, """
            class C
            {
                void M()
                {
            [|
                    var i = 1;
            |]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CommentMarkerInString()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|//string s = '\\';|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|string s = '\\';|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_MultipleCommentedLinesSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|//var i = 1;
                    //var j = 2;|]
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
    public void RemoveComment_MultipleCommentedLinesAndWhitespaceSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
            [|
                    //var i = 1;

                    //var j = 2;
                |]
                }
            }
            """, """
            class C
            {
                void M()
                {
            [|
                    var i = 1;

                    var j = 2;
            |]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_MultipleCommentedLinesWithEndCommentSelected()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|//var i = 1; // A comment.
                    //var j = 2;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1; // A comment.
                    var j = 2;|]
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_MultipleLinesWithDifferentIndentationsSelected()
        => ToggleComment("""
            class C
            {
            [|    //void M()
                //{
                //    var i = 1;

                //    var j = 2;
                //}|]
            }
            """, """
            class C
            {
            [|    void M()
                {
                    var i = 1;

                    var j = 2;
                }|]
            }
            """);

    [WpfFact]
    public void RemoveComment_MultiCaret()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    //var [||]i = 1;
                          [||]
                    //var [||]j = 2;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var [||]i = 1;
            [||]
                    var [||]j = 2;
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_MultiSeletion()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|//var i = 1;|]
                    [|            |]
                    [|//var j = 2;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|var i = 1;|]
            [||]
                    [|var j = 2;|]
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
                    [|//var i = 1;|]
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
    public void ToggleComment_MultipleLinesSelected()
    {
        var expected = new string[]
        {
            """
            class C
            {
                void M()
                {
                    [|////var i = 1;

                    //var j = 2;|]
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    [|//var i = 1;

                    var j = 2;|]
                }
            }
            """
    };

        ToggleCommentMultiple("""
            class C
            {
                void M()
                {
                    [|//var i = 1;

                    var j = 2;|]
                }
            }
            """, expected);
    }

    [WpfFact]
    public void ToggleComment_MultipleSelection()
    {
        var expected = new string[]
        {
            """
            class C
            {
                void M()
                {
                    [|////var i = |]1;
            [||]
                    [|//var j = 2;|]
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    [|//var i = |]1;
            [||]
                    [|var j = 2;|]
                }
            }
            """
    };

        ToggleCommentMultiple("""
            class C
            {
                void M()
                {
                    [|//var i = |]1;
            [||]
                    [|var j = 2;|]
                }
            }
            """, expected);
    }

    internal override AbstractCommentSelectionBase<ValueTuple> GetToggleCommentCommandHandler(EditorTestWorkspace workspace)
    {
        return (AbstractCommentSelectionBase<ValueTuple>)workspace.ExportProvider.GetExportedValues<ICommandHandler>()
            .First(export => typeof(ToggleLineCommentCommandHandler).Equals(export.GetType()));
    }

    internal override EditorTestWorkspace GetWorkspace(string markup, TestComposition composition)
        => EditorTestWorkspace.CreateCSharp(markup, composition: composition);
}
