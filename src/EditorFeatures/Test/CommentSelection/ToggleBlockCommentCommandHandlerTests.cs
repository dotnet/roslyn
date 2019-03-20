// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CommentSelection
{
    [UseExportProvider]
    public class ToggleBlockCommentCommandHandlerTests : AbstractToggleBlockCommentTestBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
        public void AddComment_EmptyCaret()
        {
            var markup = @"$$";
            var expected = @"/**/";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(0, 4)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
        public void AddComment_EmptySelection()
        {
            var markup = @"[| |]";
            var expected = @"/* */";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(0, 5)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 57)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
/*        var i = 1;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(35, 57)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var/**/ i = 1;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(46, 50)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var /*i = 1*/;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(47, 56)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        va/**/r i = 1;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(45, 49)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        Func<int, bool> myFunc = x =/**/> x == 5;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(71, 75)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        var j = 2;
        var k = 3;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 97)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        var j = 2;
        var k = 3;
    
*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 105)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*//var i = 1;
        var j = 2;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 79)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        *//*var j = 2;
        var k = 3;*//*
        var l = 4;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 125)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        *//*var j = 2;
        var k = 3;*//*
        var l = 4;*//*
        var m = 5;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 149)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        *//*var j = 2;
        var k = 3;*/

    /*
        var l = 4;*//*
        var m = 5;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 157)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        var j = 2;*//*
        var k = 3;
        *//*var l = 4;
        var m = 5;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 145)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        *//*var j = 2;
        var k = 3;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 101)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        *//*var j = 2;
        var k = 3;*/
  
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 105)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        var j = 2;*/
        var k = 3;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 99)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        *//*var j = 2;
        var*//* k = 3;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 94)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        var j = 2;*//*
        var k = 3;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 101)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        var j = 2;*//*
        var k = 3;
  */
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(35, 105)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var j = 2;*//*
        var k = 3;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(75, 101)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        var j = 2;*//*
        var k = 3;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 79)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var *//*j = 2;*//*
        var k = 3;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(71, 105)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
    {/**/
        /*var i = 1;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(33, 37)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
    /**/    var /*i*/ = 1;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(39, 43)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
/**/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(58, 62)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var */i = 1;  /**/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(59, 63)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;*/
        /*var j = 2;*/
        /*var k = 3;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 57),
                Span.FromBounds(67, 81),
                Span.FromBounds(91, 105)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;*//*
        *//*var j = 2;*/
        /*var k = 3;  */
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 57),
                Span.FromBounds(71, 85),
                Span.FromBounds(95, 111)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
#if false
        var j = 2;
#endif
        var k = 3;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 116),
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
        public void RemoveComment_AtBeginningOfFile()
        {
            var markup = @"[|/**/|]";
            var expected = @"";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(0, 0)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1;
        var j = 2;
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 93)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1;
        var j = 2;/*
        var k = 3;*/
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 73)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1;
        var/* j = 2;*/
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 66)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1/**/;
        var/* j = 2;*/
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 46)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1;
        var j = 2;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 73)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var*/ j = 2;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(73, 77)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1;
        var j = 2;
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 103)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1;
        var j = 2;
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 93)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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

        var i = 1;
        var j = 2;
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(45, 95)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1;
        //var j = 2;
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 95)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1;
        var j = 2;
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 93)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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

        var i = 1;

        var j = 2;
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(45, 97)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1;
        var j = 2;
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 93)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1;
        var j = 2;
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 93)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1;
        var j = 2;
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 53),
                Span.FromBounds(63, 73),
                Span.FromBounds(83, 93)
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        var i = 1;
        var j = 2;
        var k = 3;
    }
}";

            var expectedSelectedSpans = new[]
            {
                Span.FromBounds(43, 93),
            };
            ToggleBlockComment(markup, expected, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        /*var i = 1;
        var j = 2;
        var k = 3;*/
    }
}",
@"
class C
{
    void M()
    {
        var i = 1;
        var j = 2;
        var k = 3;
    }
}"
            };

            var expectedSelectedSpans = new[]
            {
                new[]
                {
                    Span.FromBounds(43, 97)
                },
                new[]
                {
                    Span.FromBounds(43, 93)
                }
            };

            ToggleBlockCommentMultiple(markup, expectedText, expectedSelectedSpans);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
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
        *//*var *//* j = 2;
        var k = 3;*/
    }
}",
@"
class C
{
    void M()
    {
        /*var i = 1;
        */
        var j = 2;
        var k = 3;
    }
}"
            };

            var expectedSelectedSpans = new[]
            {
                new[]
                {
                    Span.FromBounds(67, 106)
                },
                new[]
                {
                    Span.FromBounds(77, 107)
                }
            };

            ToggleBlockCommentMultiple(markup, expectedText, expectedSelectedSpans);
        }

        internal override ToggleBlockCommentCommandHandler GetToggleBlockCommentCommandHandler(TestWorkspace workspace)
        {
            return new ToggleBlockCommentCommandHandler(
                    workspace.ExportProvider.GetExportedValue<ITextUndoHistoryRegistry>(),
                    workspace.ExportProvider.GetExportedValue<IEditorOperationsFactoryService>());
        }
    }
}
