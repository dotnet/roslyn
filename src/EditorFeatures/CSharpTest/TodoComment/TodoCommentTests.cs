// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TodoComment
{
    public class TodoCommentTests
    {
        [WpfFact]
        public void SingleLineTodoComment_Colon()
        {
            var code = @"// [|TODO:test|]";

            Test(code);
        }

        [WpfFact]
        public void SingleLineTodoComment_Space()
        {
            var code = @"// [|TODO test|]";

            Test(code);
        }

        [WpfFact]
        public void SingleLineTodoComment_Underscore()
        {
            var code = @"// TODO_test";

            Test(code);
        }

        [WpfFact]
        public void SingleLineTodoComment_Number()
        {
            var code = @"// TODO1 test";

            Test(code);
        }

        [WpfFact]
        public void SingleLineTodoComment_Quote()
        {
            var code = @"// ""TODO test""";

            Test(code);
        }

        [WpfFact]
        public void SingleLineTodoComment_Middle()
        {
            var code = @"// Hello TODO test";

            Test(code);
        }

        [WpfFact]
        public void SingleLineTodoComment_Document()
        {
            var code = @"///    [|TODO test|]";

            Test(code);
        }

        [WpfFact]
        public void SingleLineTodoComment_Preprocessor1()
        {
            var code = @"#if DEBUG // [|TODO test|]";

            Test(code);
        }

        [WpfFact]
        public void SingleLineTodoComment_Preprocessor2()
        {
            var code = @"#if DEBUG ///    [|TODO test|]";

            Test(code);
        }

        [WpfFact]
        public void SingleLineTodoComment_Region()
        {
            var code = @"#region // TODO test";

            Test(code);
        }

        [WpfFact]
        public void SingleLineTodoComment_EndRegion()
        {
            var code = @"#endregion // [|TODO test|]";

            Test(code);
        }

        [WpfFact]
        public void SingleLineTodoComment_TrailingSpan()
        {
            var code = @"// [|TODO test                        |]";

            Test(code);
        }

        [WpfFact]
        public void MultilineTodoComment_Singleline()
        {
            var code = @"/* [|TODO: hello    |]*/";

            Test(code);
        }

        [WpfFact]
        public void MultilineTodoComment_Singleline_Document()
        {
            var code = @"/** [|TODO: hello    |]*/";

            Test(code);
        }

        [WpfFact]
        public void MultilineTodoComment_Multiline()
        {
            var code = @"
/* [|TODO: hello    |]
        [|TODO: hello    |]
[|TODO: hello    |]
    * [|TODO: hello    |]
    [|TODO: hello    |]*/";

            Test(code);
        }

        [WpfFact]
        public void MultilineTodoComment_Multiline_DocComment()
        {
            var code = @"
/** [|TODO: hello    |]
        [|TODO: hello    |]
[|TODO: hello    |]
    * [|TODO: hello    |]
    [|TODO: hello    |]*/";

            Test(code);
        }

        [WpfFact]
        public void SinglelineDocumentComment_Multiline()
        {
            var code = @"
        /// <summary>
        /// [|TODO : test       |]
        /// </summary>
        ///         [|UNDONE: test2             |]";

            Test(code);
        }

        private static void Test(string codeWithMarker)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(codeWithMarker))
            {
                var commentTokens = new TodoCommentTokens();
                var provider = new TodoCommentIncrementalAnalyzerProvider(commentTokens);
                var worker = (TodoCommentIncrementalAnalyzer)provider.CreateIncrementalAnalyzer(workspace);

                var document = workspace.Documents.First();
                var documentId = document.Id;
                var reasons = new InvocationReasons(PredefinedInvocationReasons.DocumentAdded);
                worker.AnalyzeSyntaxAsync(workspace.CurrentSolution.GetDocument(documentId), CancellationToken.None).Wait();

                var todoLists = worker.GetItems_TestingOnly(documentId);
                var expectedLists = document.SelectedSpans;

                Assert.Equal(todoLists.Length, expectedLists.Count);

                for (int i = 0; i < todoLists.Length; i++)
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
