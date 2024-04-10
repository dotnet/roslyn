// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.CommentSelection;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CommentSelection
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.ToggleLineComment)]
    public class CSharpToggleLineCommentCommandHandlerTests : AbstractToggleCommentTestBase
    {
        [WpfFact]
        public void AddComment_EmptyCaret()
        {
            var markup = @"$$";
            var expected = @"[||]";

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_EmptySelection()
        {
            var markup = @"[| |]";
            var expected = @"[||]";

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_CaretInUncommentedLine()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        var$$ i = 1;
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        //var[||] i = 1;
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_CaretBeforeUncommentedLine()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                $$        var i = 1;
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                [||]        //var i = 1;
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_SingleLineSelected()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|var i = 1;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|//var i = 1;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_PartialSingleLineSelected()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        var [|i = 1;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        //var [|i = 1;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_SingleLineWithWhitespaceSelected()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                [|
                        var i = 1;
                   |]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                [|
                        //var i = 1;
                   |]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_SelectionInsideCommentAtEndOfLine()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        var i = 1; // A [|comment|].
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        //var i = 1; // A [|comment|].
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_SelectionAroundCommentAtEndOfLine()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        var i = 1; [|// A comment.|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        //var i = 1; [|// A comment.|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_SelectionOutsideCommentAtEndOfLine()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|var i = 1; // A comment.|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|//var i = 1; // A comment.|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_CaretOutsideCommentAtEndOfLine()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        var $$i = 1; // A comment.
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        //var [||]i = 1; // A comment.
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_CaretInsideCommentAtEndOfLine()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        var i = 1; // A $$comment.
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        //var i = 1; // A [||]comment.
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_CommentMarkerInString()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|string s = '\\';|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|//string s = '\\';|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_MultipleLinesSelected()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|var i = 1;
                        var j = 2;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|//var i = 1;
                        //var j = 2;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_MultipleLinesWithWhitespaceSelected()
        {
            var markup =
                """
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
                """;
            var expected =
                """
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
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_MultipleLinesPartiallyCommentedSelected()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|//var i = 1;
                        var j = 2;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|////var i = 1;
                        //var j = 2;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_MultipleLinesWithCommentsInLineSelected()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|var i = 1; // A comment.
                        var j = 2;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|//var i = 1; // A comment.
                        //var j = 2;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_MultipleLinesWithDifferentIndentationsSelected()
        {
            var markup =
                """
                class C
                {
                [|    void M()
                    {
                        var i = 1;

                        var j = 2;
                    }|]
                }
                """;
            var expected =
                """
                class C
                {
                [|    //void M()
                    //{
                    //    var i = 1;

                    //    var j = 2;
                    //}|]
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_MultiCaret()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        var [||]i = 1;
                        var [||]j = 2;
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        //var [||]i = 1;
                        //var [||]j = 2;
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_MultiSeletion()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|var i = 1;|]
                        [|var j = 2;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|//var i = 1;|]
                        [|//var j = 2;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_MultiSeletionPartiallyCommented()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|//var i = |]1;
                        [|var j = 2;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|////var i = |]1;
                        [|//var j = 2;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_WithProjectionBuffer()
        {
            var surfaceMarkup = @"&lt; html &gt;@{|S1:|}";
            var csharpMarkup =
                """
                {|S1:class C
                {
                    void M()
                    {
                        [|var i = 1;|]
                    }
                }|}
                """;
            var expected =
                """
                &lt; html &gt;@class C
                {
                    void M()
                    {
                        [|//var i = 1;|]
                    }
                }
                """;

            ToggleCommentWithProjectionBuffer(surfaceMarkup, csharpMarkup, expected);
        }

        [WpfFact]
        public void RemoveComment_CaretInCommentedLine()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        //var$$ i = 1;
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        var[||] i = 1;
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_CaretBeforeCommentedLine()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                    $$    //var i = 1;
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                    [||]    var i = 1;
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_CaretInCommentedLineWithEndComment()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        //var i = 1; // A $$comment.
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        var i = 1; // A [||]comment.
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_CaretInDoubleCommentedLine()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        ////var$$ i = 1;
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        //var[||] i = 1;
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_CommentedLineSelected()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|//var i = 1;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|var i = 1;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_InsideCommentSelected()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        //var [|i = 1;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        var [|i = 1;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_CommentedLineWithWhitespaceSelected()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                [|
                        //var i = 1;
                  |]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                [|
                        var i = 1;
                |]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_CommentMarkerInString()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|//string s = '\\';|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|string s = '\\';|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_MultipleCommentedLinesSelected()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|//var i = 1;
                        //var j = 2;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|var i = 1;
                        var j = 2;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_MultipleCommentedLinesAndWhitespaceSelected()
        {
            var markup =
                """
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
                """;
            var expected =
                """
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
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_MultipleCommentedLinesWithEndCommentSelected()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|//var i = 1; // A comment.
                        //var j = 2;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|var i = 1; // A comment.
                        var j = 2;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_MultipleLinesWithDifferentIndentationsSelected()
        {
            var markup =
                """
                class C
                {
                [|    //void M()
                    //{
                    //    var i = 1;

                    //    var j = 2;
                    //}|]
                }
                """;
            var expected =
                """
                class C
                {
                [|    void M()
                    {
                        var i = 1;

                        var j = 2;
                    }|]
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_MultiCaret()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        //var [||]i = 1;
                              [||]
                        //var [||]j = 2;
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        var [||]i = 1;
                [||]
                        var [||]j = 2;
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_MultiSeletion()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|//var i = 1;|]
                        [|            |]
                        [|//var j = 2;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|var i = 1;|]
                [||]
                        [|var j = 2;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_WithProjectionBuffer()
        {
            var surfaceMarkup = @"&lt; html &gt;@{|S1:|}";
            var csharpMarkup =
                """
                {|S1:class C
                {
                    void M()
                    {
                        [|//var i = 1;|]
                    }
                }|}
                """;
            var expected =
                """
                &lt; html &gt;@class C
                {
                    void M()
                    {
                        [|var i = 1;|]
                    }
                }
                """;

            ToggleCommentWithProjectionBuffer(surfaceMarkup, csharpMarkup, expected);
        }

        [WpfFact]
        public void ToggleComment_MultipleLinesSelected()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|//var i = 1;

                        var j = 2;|]
                    }
                }
                """;
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

            ToggleCommentMultiple(markup, expected);
        }

        [WpfFact]
        public void ToggleComment_MultipleSelection()
        {
            var markup =
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
                """;
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

            ToggleCommentMultiple(markup, expected);
        }

        internal override AbstractCommentSelectionBase<ValueTuple> GetToggleCommentCommandHandler(EditorTestWorkspace workspace)
        {
            return (AbstractCommentSelectionBase<ValueTuple>)workspace.ExportProvider.GetExportedValues<ICommandHandler>()
                .First(export => typeof(ToggleLineCommentCommandHandler).Equals(export.GetType()));
        }

        internal override EditorTestWorkspace GetWorkspace(string markup, TestComposition composition)
            => EditorTestWorkspace.CreateCSharp(markup, composition: composition);
    }
}
