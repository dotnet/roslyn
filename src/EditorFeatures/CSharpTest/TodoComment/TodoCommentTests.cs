// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.RemoteHost;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TodoComment
{
    [UseExportProvider]
    public class TodoCommentTests
    {
        [Fact]
        public async Task SingleLineTodoComment_Colon()
        {
            var code = @"// [|TODO:test|]";

            await TestAsync(code);
        }

        [Fact]
        public async Task SingleLineTodoComment_Space()
        {
            var code = @"// [|TODO test|]";

            await TestAsync(code);
        }

        [Fact]
        public async Task SingleLineTodoComment_Underscore()
        {
            var code = @"// TODO_test";

            await TestAsync(code);
        }

        [Fact]
        public async Task SingleLineTodoComment_Number()
        {
            var code = @"// TODO1 test";

            await TestAsync(code);
        }

        [Fact]
        public async Task SingleLineTodoComment_Quote()
        {
            var code = @"// ""TODO test""";

            await TestAsync(code);
        }

        [Fact]
        public async Task SingleLineTodoComment_Middle()
        {
            var code = @"// Hello TODO test";

            await TestAsync(code);
        }

        [Fact]
        public async Task SingleLineTodoComment_Document()
        {
            var code = @"///    [|TODO test|]";

            await TestAsync(code);
        }

        [Fact]
        public async Task SingleLineTodoComment_Preprocessor1()
        {
            var code = @"#if DEBUG // [|TODO test|]";

            await TestAsync(code);
        }

        [Fact]
        public async Task SingleLineTodoComment_Preprocessor2()
        {
            var code = @"#if DEBUG ///    [|TODO test|]";

            await TestAsync(code);
        }

        [Fact]
        public async Task SingleLineTodoComment_Region()
        {
            var code = @"#region // TODO test";

            await TestAsync(code);
        }

        [Fact]
        public async Task SingleLineTodoComment_EndRegion()
        {
            var code = @"#endregion // [|TODO test|]";

            await TestAsync(code);
        }

        [Fact]
        public async Task SingleLineTodoComment_TrailingSpan()
        {
            var code = @"// [|TODO test                        |]";

            await TestAsync(code);
        }

        [Fact]
        public async Task MultilineTodoComment_Singleline()
        {
            var code = @"/* [|TODO: hello    |]*/";

            await TestAsync(code);
        }

        [Fact]
        public async Task MultilineTodoComment_Singleline_Document()
        {
            var code = @"/** [|TODO: hello    |]*/";

            await TestAsync(code);
        }

        [Fact]
        public async Task MultilineTodoComment_Multiline()
        {
            var code = @"
/* [|TODO: hello    |]
        [|TODO: hello    |]
[|TODO: hello    |]
    * [|TODO: hello    |]
    [|TODO: hello    |]*/";

            await TestAsync(code);
        }

        [Fact]
        public async Task MultilineTodoComment_Multiline_DocComment()
        {
            var code = @"
/** [|TODO: hello    |]
        [|TODO: hello    |]
[|TODO: hello    |]
    * [|TODO: hello    |]
    [|TODO: hello    |]*/";

            await TestAsync(code);
        }

        [Fact]
        public async Task SinglelineDocumentComment_Multiline()
        {
            var code = @"
        /// <summary>
        /// [|TODO : test       |]
        /// </summary>
        ///         [|UNDONE: test2             |]";

            await TestAsync(code);
        }

        private static async Task TestAsync(string codeWithMarker)
        {
            await TestAsync(codeWithMarker, remote: false);
            await TestAsync(codeWithMarker, remote: true);
        }

        private static async Task TestAsync(string codeWithMarker, bool remote)
        {
            using (var workspace = TestWorkspace.CreateCSharp(codeWithMarker, openDocuments: false))
            {
                workspace.Options = workspace.Options.WithChangedOption(RemoteHostOptions.RemoteHostTest, remote);

                var commentTokens = new TodoCommentTokens();
                var provider = new TodoCommentIncrementalAnalyzerProvider(commentTokens, Array.Empty<Lazy<IEventListener, EventListenerMetadata>>());
                var worker = (TodoCommentIncrementalAnalyzer)provider.CreateIncrementalAnalyzer(workspace);

                var document = workspace.Documents.First();
                var documentId = document.Id;
                var reasons = new InvocationReasons(PredefinedInvocationReasons.DocumentAdded);
                await worker.AnalyzeSyntaxAsync(workspace.CurrentSolution.GetDocument(documentId), InvocationReasons.Empty, CancellationToken.None);

                var todoLists = worker.GetItems_TestingOnly(documentId);
                var expectedLists = document.SelectedSpans;

                Assert.Equal(todoLists.Length, expectedLists.Count);

                for (var i = 0; i < todoLists.Length; i++)
                {
                    var todo = todoLists[i];
                    var span = expectedLists[i];

                    var line = document.InitialTextSnapshot.GetLineFromPosition(span.Start);
                    var text = document.InitialTextSnapshot.GetText(span.ToSpan());

                    Assert.Equal(todo.MappedLine, line.LineNumber);
                    Assert.Equal(todo.MappedColumn, span.Start - line.Start);
                    Assert.Equal(todo.Message, text);
                }
            }
        }
    }
}
