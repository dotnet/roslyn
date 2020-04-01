// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.TodoComments;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.TodoComments
{
    public abstract class AbstractTodoCommentTests
    {
        protected abstract TestWorkspace CreateWorkspace(string codeWithMarker);

        protected async Task TestAsync(string codeWithMarker)
        {
            using var workspace = CreateWorkspace(codeWithMarker);

            var hostDocument = workspace.Documents.First();
            var initialTextSnapshot = hostDocument.GetTextBuffer().CurrentSnapshot;
            var documentId = hostDocument.Id;

            var document = workspace.CurrentSolution.GetDocument(documentId);
            var service = document.GetLanguageService<ITodoCommentService>();
            var todoLists = await service.GetTodoCommentsAsync(document,
                TodoCommentDescriptor.Parse(TodoCommentOptions.TokenList.DefaultValue),
                CancellationToken.None);

            var expectedLists = hostDocument.SelectedSpans;
            Assert.Equal(todoLists.Length, expectedLists.Count);

            var sourceText = await document.GetTextAsync();
            var tree = await document.GetSyntaxTreeAsync();
            for (var i = 0; i < todoLists.Length; i++)
            {
                var todo = todoLists[i];
                var span = expectedLists[i];

                var line = initialTextSnapshot.GetLineFromPosition(span.Start);
                var text = initialTextSnapshot.GetText(span.ToSpan());

                var converted = todo.CreateSerializableData(document, sourceText, tree);

                Assert.Equal(converted.MappedLine, line.LineNumber);
                Assert.Equal(converted.MappedColumn, span.Start - line.Start);
                Assert.Equal(converted.Message, text);
            }
        }
    }
}
