// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Editor.CSharp.CommentSelection;
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
    [Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
    public class CSharpToggleBlockCommentCommandHandlerTests : AbstractToggleCommentTestBase
    {
        [WpfFact]
        public void AddComment_CommentMarkerStringBeforeSelection()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        string s = '/*';
                        [|var j = 2;
                        var k = 3;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        string s = '/*';
                        [|/*var j = 2;
                        var k = 3;*/|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_DirectiveWithCommentInsideSelection()
        {
            var markup =
                """
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
                }
                """;
            var expected =
                """
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
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_MarkerInsideSelection()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|var i = 1;
                        string s = '/*';
                        var k = 3;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|/*var i = 1;
                        string s = '/*';
                        var k = 3;*/|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_CloseCommentMarkerStringInSelection()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|var i = 1;
                        string s = '*/';
                        var k = 3;|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|/*var i = 1;
                        string s = '*/';
                        var k = 3;*/|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void AddComment_CommentMarkerStringAfterSelection()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|var i = 1;
                        var j = 2;|]
                        string s = '*/';
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|/*var i = 1;
                        var j = 2;*/|]
                        string s = '*/';
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_CommentMarkerStringNearSelection()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        string s = '/*';
                        [|/*var i = 1;
                        var j = 2;
                        var k = 3;*/|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        string s = '/*';
                        [|var i = 1;
                        var j = 2;
                        var k = 3;|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        [WpfFact]
        public void RemoveComment_CommentMarkerStringInSelection()
        {
            var markup =
                """
                class C
                {
                    void M()
                    {
                        [|/*string s = '/*';*/|]
                    }
                }
                """;
            var expected =
                """
                class C
                {
                    void M()
                    {
                        [|string s = '/*';|]
                    }
                }
                """;

            ToggleComment(markup, expected);
        }

        internal override AbstractCommentSelectionBase<ValueTuple> GetToggleCommentCommandHandler(EditorTestWorkspace workspace)
        {
            return (AbstractCommentSelectionBase<ValueTuple>)workspace.ExportProvider.GetExportedValues<ICommandHandler>()
                .First(export => typeof(CSharpToggleBlockCommentCommandHandler).Equals(export.GetType()));
        }

        internal override EditorTestWorkspace GetWorkspace(string markup, TestComposition composition)
            => EditorTestWorkspace.CreateCSharp(markup, composition: composition);
    }
}
