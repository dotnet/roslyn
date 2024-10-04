// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.TaskList
{
    public abstract class AbstractTaskListTests
    {
        private static readonly TestComposition s_inProcessComposition = EditorTestCompositions.EditorFeatures;
        private static readonly TestComposition s_outOffProcessComposition = s_inProcessComposition.WithTestHostParts(TestHost.OutOfProcess);

        protected EditorTestWorkspace CreateWorkspace(string codeWithMarker, TestHost host)
            => CreateWorkspace(codeWithMarker, host == TestHost.OutOfProcess ? s_outOffProcessComposition : s_inProcessComposition);

        protected abstract EditorTestWorkspace CreateWorkspace(string codeWithMarker, TestComposition testComposition);

        protected async Task TestAsync(string codeWithMarker, TestHost host)
        {
            using var workspace = CreateWorkspace(codeWithMarker, host);

            var descriptors = TaskListOptions.Default.Descriptors;
            workspace.GlobalOptions.SetGlobalOption(TaskListOptionsStorage.Descriptors, descriptors);

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

                Assert.Equal(todo.MappedSpan.StartLinePosition.Line, line.LineNumber);
                Assert.Equal(todo.MappedSpan.StartLinePosition.Character, span.Start - line.Start);
                Assert.Equal(todo.Message, text);
            }
        }
    }
}
