// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.CommentSelection;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
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
    public class CSharpToggleBlockCommentCommandHandlerTests : AbstractToggleCommentTestBase
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

            ToggleComment(markup, expected);
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

            ToggleComment(markup, expected);
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

            ToggleComment(markup, expected);
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
        string s = '*/';
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
        string s = '*/';
        var k = 3;*/|]
    }
}";

            ToggleComment(markup, expected);
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

            ToggleComment(markup, expected);
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

            ToggleComment(markup, expected);
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

            ToggleComment(markup, expected);
        }

        internal override AbstractCommentSelectionBase<ValueTuple> GetToggleCommentCommandHandler(TestWorkspace workspace)
        {
            return (AbstractCommentSelectionBase<ValueTuple>)workspace.ExportProvider.GetExportedValues<ICommandHandler>()
                .First(export => typeof(CSharpToggleBlockCommentCommandHandler).Equals(export.GetType()));
        }

        internal override TestWorkspace GetWorkspace(string markup, ExportProvider exportProvider)
            => TestWorkspace.CreateCSharp(markup, exportProvider: exportProvider);
    }
}
