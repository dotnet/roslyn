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
    public async Task SingleLineTodoComment_Colon(TestHost host)
    {
        await TestAsync(@"// [|TODO:test|]", host);
    }

    [Theory, CombinatorialData]
    public async Task SingleLineTodoComment_Space(TestHost host)
    {
        await TestAsync(@"// [|TODO test|]", host);
    }

    [Theory, CombinatorialData]
    public async Task SingleLineTodoComment_Underscore(TestHost host)
    {
        await TestAsync(@"// TODO_test", host);
    }

    [Theory, CombinatorialData]
    public async Task SingleLineTodoComment_Number(TestHost host)
    {
        await TestAsync(@"// TODO1 test", host);
    }

    [Theory, CombinatorialData]
    public async Task SingleLineTodoComment_Quote(TestHost host)
    {
        await TestAsync("""
            // "TODO test"
            """, host);
    }

    [Theory, CombinatorialData]
    public async Task SingleLineTodoComment_Middle(TestHost host)
    {
        await TestAsync(@"// Hello TODO test", host);
    }

    [Theory, CombinatorialData]
    public async Task SingleLineTodoComment_Document(TestHost host)
    {
        await TestAsync(@"///    [|TODO test|]", host);
    }

    [Theory, CombinatorialData]
    public async Task SingleLineTodoComment_Preprocessor1(TestHost host)
    {
        await TestAsync(@"#if DEBUG // [|TODO test|]", host);
    }

    [Theory, CombinatorialData]
    public async Task SingleLineTodoComment_Preprocessor2(TestHost host)
    {
        await TestAsync(@"#if DEBUG ///    [|TODO test|]", host);
    }

    [Theory, CombinatorialData]
    public async Task SingleLineTodoComment_Region(TestHost host)
    {
        await TestAsync(@"#region // TODO test", host);
    }

    [Theory, CombinatorialData]
    public async Task SingleLineTodoComment_EndRegion(TestHost host)
    {
        await TestAsync(@"#endregion // [|TODO test|]", host);
    }

    [Theory, CombinatorialData]
    public async Task SingleLineTodoComment_TrailingSpan(TestHost host)
    {
        await TestAsync(@"// [|TODO test                        |]", host);
    }

    [Theory, CombinatorialData]
    public async Task MultilineTodoComment_Singleline(TestHost host)
    {
        await TestAsync(@"/* [|TODO: hello    |]*/", host);
    }

    [Theory, CombinatorialData]
    public async Task MultilineTodoComment_Singleline_Document(TestHost host)
    {
        await TestAsync(@"/** [|TODO: hello    |]*/", host);
    }

    [Theory, CombinatorialData]
    public async Task MultilineTodoComment_Multiline(TestHost host)
    {
        await TestAsync("""
            /* [|TODO: hello    |]
                    [|TODO: hello    |]
            [|TODO: hello    |]
                * [|TODO: hello    |]
                [|TODO: hello    |]*/
            """, host);
    }

    [Theory, CombinatorialData]
    public async Task MultilineTodoComment_Multiline_DocComment(TestHost host)
    {
        await TestAsync("""
            /** [|TODO: hello    |]
                    [|TODO: hello    |]
            [|TODO: hello    |]
                * [|TODO: hello    |]
                [|TODO: hello    |]*/
            """, host);
    }

    [Theory, CombinatorialData]
    public async Task SinglelineDocumentComment_Multiline(TestHost host)
    {
        await TestAsync("""
            /// <summary>
            /// [|TODO : test       |]
            /// </summary>
            ///         [|UNDONE: test2             |]
            """, host);
    }
}
