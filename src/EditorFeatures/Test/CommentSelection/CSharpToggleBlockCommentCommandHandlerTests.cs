// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.CommentSelection;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;
using Xunit;
using System.Linq;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CommentSelection
{
    [UseExportProvider]
    public class CSharpToggleBlockCommentCommandHandlerTests : AbstractToggleBlockCommentTestBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
        public void AddComment_CommentMarkerStringBeforeSelection()
        {
            var markup =
@"
class C
{
    void M()
    {
        string s = '/*';
        [|var j = 2;
        var k = 3;|]
    }
}";
            var expected =
@"
class C
{
    void M()
    {
        string s = '/*';
        [|/*var j = 2;
        var k = 3;*/|]
    }
}";

            ToggleBlockComment(markup, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
        public void AddComment_DirectiveWithCommentInsideSelection()
        {
            var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
#if false
        /*var j = 2;*/
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
        /*var j = 2;*/
#endif
        var k = 3;*/|]
    }
}";

            ToggleBlockComment(markup, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
        public void AddComment_MarkerInsideSelection()
        {
            var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        string s = '/*';
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
        string s = '/*';
        var k = 3;*/|]
    }
}";

            ToggleBlockComment(markup, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
        public void AddComment_CloseCommentMarkerStringInSelection()
        {
            var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        string s = '/*';
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
        string s = '/*';
        var k = 3;*/|]
    }
}";

            ToggleBlockComment(markup, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
        public void AddComment_CommentMarkerStringAfterSelection()
        {
            var markup =
@"
class C
{
    void M()
    {
        [|var i = 1;
        var j = 2;|]
        string s = '*/';
    }
}";
            var expected =
@"
class C
{
    void M()
    {
        [|/*var i = 1;
        var j = 2;*/|]
        string s = '*/';
    }
}";

            ToggleBlockComment(markup, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
        public void RemoveComment_CommentMarkerStringNearSelection()
        {
            var markup =
@"
class C
{
    void M()
    {
        string s = '/*';
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
        string s = '/*';
        [|var i = 1;
        var j = 2;
        var k = 3;|]
    }
}";

            ToggleBlockComment(markup, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
        public void RemoveComment_CommentMarkerStringInSelection()
        {
            var markup =
@"
class C
{
    void M()
    {
        [|/*string s = '/*';*/|]
    }
}";
            var expected =
@"
class C
{
    void M()
    {
        [|string s = '/*';|]
    }
}";

            ToggleBlockComment(markup, expected);
        }

        internal override ToggleBlockCommentCommandHandler GetToggleBlockCommentCommandHandler(TestWorkspace workspace)
        {
            return (ToggleBlockCommentCommandHandler)workspace.ExportProvider.GetExportedValues<VSCommanding.ICommandHandler>()
                .First(export => typeof(CSharpToggleBlockCommentCommandHandler).Equals(export.GetType()));
        }
    }
}
