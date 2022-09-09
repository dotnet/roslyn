// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.TaskList
{
    public abstract class AbstractTaskListTests
    {
        protected abstract TestWorkspace CreateWorkspace(string codeWithMarker);

        protected async Task TestAsync(string codeWithMarker)
        {
            using var workspace = CreateWorkspace(codeWithMarker);

            var descriptors = TaskListOptions.Default.Descriptors;
            workspace.GlobalOptions.SetGlobalOption(new OptionKey(TaskListOptionsStorage.Descriptors), descriptors);

            var hostDocument = workspace.Documents.First();
            var initialTextSnapshot = hostDocument.GetTextBuffer().CurrentSnapshot;
            var documentId = hostDocument.Id;

            var document = workspace.CurrentSolution.GetRequiredDocument(documentId);
            var service = document.GetRequiredLanguageService<ITaskListService>();
            var items = await service.GetTaskListItemsAsync(document, TaskListItemDescriptor.Parse(descriptors), CancellationToken.None);

            var expectedLists = hostDocument.SelectedSpans;
            Assert.Equal(items.Length, expectedLists.Count);

            var sourceText = await document.GetTextAsync();
            var tree = await document.GetSyntaxTreeAsync();
            for (var i = 0; i < items.Length; i++)
            {
                var todo = items[i];
                var span = expectedLists[i];

                var line = initialTextSnapshot.GetLineFromPosition(span.Start);
                var text = initialTextSnapshot.GetText(span.ToSpan());

                Assert.Equal(todo.MappedLine, line.LineNumber);
                Assert.Equal(todo.MappedColumn, span.Start - line.Start);
                Assert.Equal(todo.Message, text);
            }
        }
    }
}
