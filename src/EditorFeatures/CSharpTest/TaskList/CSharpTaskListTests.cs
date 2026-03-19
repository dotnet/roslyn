// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.TaskList;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TaskList;

[UseExportProvider]
public sealed class CSharpTaskListTests : AbstractTaskListTests
{
    protected override EditorTestWorkspace CreateWorkspace(string codeWithMarker, TestComposition composition)
        => EditorTestWorkspace.CreateCSharp(codeWithMarker, composition: composition);

    [Theory, CombinatorialData]
    public Task SingleLineTodoComment_Colon(TestHost host)
        => TestAsync(@"// [|TODO:test|]", host);

    [Theory, CombinatorialData]
    public Task SingleLineTodoComment_Space(TestHost host)
        => TestAsync(@"// [|TODO test|]", host);

    [Theory, CombinatorialData]
    public Task SingleLineTodoComment_Underscore(TestHost host)
        => TestAsync(@"// TODO_test", host);

    [Theory, CombinatorialData]
    public Task SingleLineTodoComment_Number(TestHost host)
        => TestAsync(@"// TODO1 test", host);

    [Theory, CombinatorialData]
    public Task SingleLineTodoComment_Quote(TestHost host)
        => TestAsync("""
            // "TODO test"
            """, host);

    [Theory, CombinatorialData]
    public Task SingleLineTodoComment_Middle(TestHost host)
        => TestAsync(@"// Hello TODO test", host);

    [Theory, CombinatorialData]
    public Task SingleLineTodoComment_Document(TestHost host)
        => TestAsync(@"///    [|TODO test|]", host);

    [Theory, CombinatorialData]
    public Task SingleLineTodoComment_Preprocessor1(TestHost host)
        => TestAsync(@"#if DEBUG // [|TODO test|]", host);

    [Theory, CombinatorialData]
    public Task SingleLineTodoComment_Preprocessor2(TestHost host)
        => TestAsync(@"#if DEBUG ///    [|TODO test|]", host);

    [Theory, CombinatorialData]
    public Task SingleLineTodoComment_Region(TestHost host)
        => TestAsync(@"#region // TODO test", host);

    [Theory, CombinatorialData]
    public Task SingleLineTodoComment_EndRegion(TestHost host)
        => TestAsync(@"#endregion // [|TODO test|]", host);

    [Theory, CombinatorialData]
    public Task SingleLineTodoComment_TrailingSpan(TestHost host)
        => TestAsync(@"// [|TODO test                        |]", host);

    [Theory, CombinatorialData]
    public Task MultilineTodoComment_Singleline(TestHost host)
        => TestAsync(@"/* [|TODO: hello    |]*/", host);

    [Theory, CombinatorialData]
    public Task MultilineTodoComment_Singleline_Document(TestHost host)
        => TestAsync(@"/** [|TODO: hello    |]*/", host);

    [Theory, CombinatorialData]
    public Task MultilineTodoComment_Multiline(TestHost host)
        => TestAsync("""
            /* [|TODO: hello    |]
                    [|TODO: hello    |]
            [|TODO: hello    |]
                * [|TODO: hello    |]
                [|TODO: hello    |]*/
            """, host);

    [Theory, CombinatorialData]
    public Task MultilineTodoComment_Multiline_DocComment(TestHost host)
        => TestAsync("""
            /** [|TODO: hello    |]
                    [|TODO: hello    |]
            [|TODO: hello    |]
                * [|TODO: hello    |]
                [|TODO: hello    |]*/
            """, host);

    [Theory, CombinatorialData]
    public Task SinglelineDocumentComment_Multiline(TestHost host)
        => TestAsync("""
            /// <summary>
            /// [|TODO : test       |]
            /// </summary>
            ///         [|UNDONE: test2             |]
            """, host);
}
