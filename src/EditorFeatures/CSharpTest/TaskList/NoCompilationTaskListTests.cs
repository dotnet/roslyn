// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.TaskList;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TaskList;

[UseExportProvider]
public sealed class NoCompilationTaskListTests : AbstractTaskListTests
{
    protected override EditorTestWorkspace CreateWorkspace(string codeWithMarker, TestComposition composition)
    {
        var workspace = EditorTestWorkspace.CreateWorkspace(XElement.Parse(
            $"""
            <Workspace>
                <Project Language="NoCompilation">
                    <Document>{codeWithMarker}</Document>
                </Project>
            </Workspace>
            """), composition: composition.AddParts(
            typeof(NoCompilationContentTypeDefinitions),
            typeof(NoCompilationContentTypeLanguageService),
            typeof(NoCompilationTaskListService)));

        return workspace;
    }

    [Theory, CombinatorialData, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1192024")]
    public Task TodoCommentInNoCompilationProject(TestHost host)
        => TestAsync(@"(* [|Message|] *)", host);
}

[PartNotDiscoverable]
[ExportLanguageService(typeof(ITaskListService), language: NoCompilationConstants.LanguageName), Shared]
internal sealed class NoCompilationTaskListService : ITaskListService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public NoCompilationTaskListService()
    {
    }

    public async Task<ImmutableArray<TaskListItem>> GetTaskListItemsAsync(Document document, ImmutableArray<TaskListItemDescriptor> descriptors, CancellationToken cancellationToken)
        => ImmutableArray.Create(new TaskListItem(
            descriptors.First().Priority,
            "Message",
            document.Id,
            Span: new FileLinePositionSpan("dummy", new LinePosition(0, 3), new LinePosition(0, 3)),
            MappedSpan: new FileLinePositionSpan("dummy", new LinePosition(0, 3), new LinePosition(0, 3))));
}
