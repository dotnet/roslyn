// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.TaskList;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TaskList
{
    [UseExportProvider]
    public class NoCompilationTaskListTests : AbstractTaskListTests
    {
        protected override TestWorkspace CreateWorkspace(string codeWithMarker, TestComposition composition)
        {
            var workspace = TestWorkspace.CreateWorkspace(XElement.Parse(
$@"<Workspace>
    <Project Language=""NoCompilation"">
        <Document>{codeWithMarker}</Document>
    </Project>
</Workspace>"), composition: composition.AddParts(
                typeof(NoCompilationContentTypeDefinitions),
                typeof(NoCompilationContentTypeLanguageService),
                typeof(NoCompilationTaskListService)));

            return workspace;
        }

        [Theory, CombinatorialData, WorkItem(1192024, "https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1192024")]
        public async Task TodoCommentInNoCompilationProject(TestHost host)
        {
            var code = @"(* [|Message|] *)";

            await TestAsync(code, host);
        }
    }

    [PartNotDiscoverable]
    [ExportLanguageService(typeof(ITodoCommentService), language: NoCompilationConstants.LanguageName), Shared]
    internal class NoCompilationTaskListService : ITodoCommentService
    {
        [ImportingConstructor]
        [System.Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NoCompilationTaskListService()
        {
        }

        public Task<ImmutableArray<CodeAnalysis.TodoComments.TodoComment>> GetTodoCommentsAsync(Document document, ImmutableArray<TodoCommentDescriptor> commentDescriptors, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray.Create(new CodeAnalysis.TodoComments.TodoComment(commentDescriptors.First(), "Message", 3)));
        }
    }
}
