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
using Microsoft.CodeAnalysis.Test.Utilities.TodoComments;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TodoComment
{
    [UseExportProvider]
    public class NoCompilationTodoCommentTests : AbstractTodoCommentTests
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
                typeof(NoCompilationTodoCommentService)));

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
    [ExportLanguageService(typeof(ITodoCommentDataService), language: NoCompilationConstants.LanguageName), Shared]
    internal class NoCompilationTodoCommentService : ITodoCommentDataService
    {
        [ImportingConstructor]
        [System.Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NoCompilationTodoCommentService()
        {
        }

        public Task<ImmutableArray<TodoCommentData>> GetTodoCommentDataAsync(Document document, ImmutableArray<TodoCommentDescriptor> commentDescriptors, CancellationToken cancellationToken)
            => Task.FromResult(ImmutableArray.Create(new TodoCommentData(
                commentDescriptors.First().Priority,
                "Message",
                document.Id,
                span: new FileLinePositionSpan("dummy", new LinePosition(0, 3), new LinePosition(0, 3)),
                mappedSpan: new FileLinePositionSpan("dummy", new LinePosition(0, 3), new LinePosition(0, 3)))));
    }
}
