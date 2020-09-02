// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.TodoComments;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TodoComment
{
    [UseExportProvider]
    public class NoCompilationTodoCommentTests : AbstractTodoCommentTests
    {
        protected override TestWorkspace CreateWorkspace(string codeWithMarker)
        {
            return TestWorkspace.CreateWorkspace(XElement.Parse(
$@"<Workspace>
    <Project Language=""NoCompilation"">
        <Document>{codeWithMarker}</Document>
    </Project>
</Workspace>"), composition: EditorTestCompositions.EditorFeatures.AddParts(
                typeof(NoCompilationContentTypeDefinitions),
                typeof(NoCompilationContentTypeLanguageService),
                typeof(NoCompilationTodoCommentService)));
        }

        [Fact, WorkItem(1192024, "https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1192024")]
        public async Task TodoCommentInNoCompilationProject()
        {
            var code = @"(* [|Message|] *)";

            await TestAsync(code);
        }
    }

    [PartNotDiscoverable]
    [ExportLanguageService(typeof(ITodoCommentService), language: NoCompilationConstants.LanguageName), Shared]
    internal class NoCompilationTodoCommentService : ITodoCommentService
    {
        [ImportingConstructor]
        [System.Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NoCompilationTodoCommentService()
        {
        }

        public Task<ImmutableArray<TodoComments.TodoComment>> GetTodoCommentsAsync(Document document, ImmutableArray<TodoCommentDescriptor> commentDescriptors, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray.Create(new TodoComments.TodoComment(commentDescriptors.First(), "Message", 3)));
        }
    }

    [UseExportProvider]
    public class TodoCommentTests : AbstractTodoCommentTests
    {
        protected override TestWorkspace CreateWorkspace(string codeWithMarker)
            => TestWorkspace.CreateCSharp(codeWithMarker);

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
    }
}
