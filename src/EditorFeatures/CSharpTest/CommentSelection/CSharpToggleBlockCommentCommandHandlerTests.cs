// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Editor.CSharp.CommentSelection;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.CommentSelection;
using Microsoft.VisualStudio.Commanding;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CommentSelection;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.ToggleBlockComment)]
public sealed class CSharpToggleBlockCommentCommandHandlerTests : AbstractToggleCommentTestBase
{
    [WpfFact]
    public void AddComment_CommentMarkerStringBeforeSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    string s = '/*';
                    [|var j = 2;
                    var k = 3;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    string s = '/*';
                    [|/*var j = 2;
                    var k = 3;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_DirectiveWithCommentInsideSelection()
        => ToggleComment("""
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
            """, """
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
            """);

    [WpfFact]
    public void AddComment_MarkerInsideSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    string s = '/*';
                    var k = 3;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    string s = '/*';
                    var k = 3;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_CloseCommentMarkerStringInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    string s = '*/';
                    var k = 3;|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    string s = '*/';
                    var k = 3;*/|]
                }
            }
            """);

    [WpfFact]
    public void AddComment_CommentMarkerStringAfterSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|var i = 1;
                    var j = 2;|]
                    string s = '*/';
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|/*var i = 1;
                    var j = 2;*/|]
                    string s = '*/';
                }
            }
            """);

    [WpfFact]
    public void RemoveComment_CommentMarkerStringNearSelection()
        => ToggleComment("""
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
            """, """
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
            """);

    [WpfFact]
    public void RemoveComment_CommentMarkerStringInSelection()
        => ToggleComment("""
            class C
            {
                void M()
                {
                    [|/*string s = '/*';*/|]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    [|string s = '/*';|]
                }
            }
            """);

    internal override AbstractCommentSelectionBase<ValueTuple> GetToggleCommentCommandHandler(EditorTestWorkspace workspace)
    {
        return (AbstractCommentSelectionBase<ValueTuple>)workspace.ExportProvider.GetExportedValues<ICommandHandler>()
            .First(export => typeof(CSharpToggleBlockCommentCommandHandler).Equals(export.GetType()));
    }

    internal override EditorTestWorkspace GetWorkspace(string markup, TestComposition composition)
        => EditorTestWorkspace.CreateCSharp(markup, composition: composition);
}
