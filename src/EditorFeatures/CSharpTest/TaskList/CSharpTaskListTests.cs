// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.TaskList;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TaskList
{
    [UseExportProvider]
    public class CSharpTaskListTests : AbstractTaskListTests
    {
        protected override EditorTestWorkspace CreateWorkspace(string codeWithMarker, TestComposition composition)
            => EditorTestWorkspace.CreateCSharp(codeWithMarker, composition: composition);

        [Theory, CombinatorialData]
        public async Task SingleLineTodoComment_Colon(TestHost host)
        {
            var code = @"// [|TODO:test|]";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task SingleLineTodoComment_Space(TestHost host)
        {
            var code = @"// [|TODO test|]";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task SingleLineTodoComment_Underscore(TestHost host)
        {
            var code = @"// TODO_test";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task SingleLineTodoComment_Number(TestHost host)
        {
            var code = @"// TODO1 test";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task SingleLineTodoComment_Quote(TestHost host)
        {
            var code = """
                // "TODO test"
                """;

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task SingleLineTodoComment_Middle(TestHost host)
        {
            var code = @"// Hello TODO test";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task SingleLineTodoComment_Document(TestHost host)
        {
            var code = @"///    [|TODO test|]";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task SingleLineTodoComment_Preprocessor1(TestHost host)
        {
            var code = @"#if DEBUG // [|TODO test|]";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task SingleLineTodoComment_Preprocessor2(TestHost host)
        {
            var code = @"#if DEBUG ///    [|TODO test|]";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task SingleLineTodoComment_Region(TestHost host)
        {
            var code = @"#region // TODO test";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task SingleLineTodoComment_EndRegion(TestHost host)
        {
            var code = @"#endregion // [|TODO test|]";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task SingleLineTodoComment_TrailingSpan(TestHost host)
        {
            var code = @"// [|TODO test                        |]";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task MultilineTodoComment_Singleline(TestHost host)
        {
            var code = @"/* [|TODO: hello    |]*/";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task MultilineTodoComment_Singleline_Document(TestHost host)
        {
            var code = @"/** [|TODO: hello    |]*/";

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task MultilineTodoComment_Multiline(TestHost host)
        {
            var code = """
                /* [|TODO: hello    |]
                        [|TODO: hello    |]
                [|TODO: hello    |]
                    * [|TODO: hello    |]
                    [|TODO: hello    |]*/
                """;

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task MultilineTodoComment_Multiline_DocComment(TestHost host)
        {
            var code = """
                /** [|TODO: hello    |]
                        [|TODO: hello    |]
                [|TODO: hello    |]
                    * [|TODO: hello    |]
                    [|TODO: hello    |]*/
                """;

            await TestAsync(code, host);
        }

        [Theory, CombinatorialData]
        public async Task SinglelineDocumentComment_Multiline(TestHost host)
        {
            var code = """
                /// <summary>
                /// [|TODO : test       |]
                /// </summary>
                ///         [|UNDONE: test2             |]
                """;

            await TestAsync(code, host);
        }
    }
}
