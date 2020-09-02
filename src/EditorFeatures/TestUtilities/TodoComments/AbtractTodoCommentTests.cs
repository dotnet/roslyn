// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
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
            var todoComments = await service.GetTodoCommentsAsync(document,
                TodoCommentDescriptor.Parse(TodoCommentOptions.TokenList.DefaultValue),
                CancellationToken.None);

            using var _ = ArrayBuilder<TodoCommentData>.GetInstance(out var converted);
            await TodoComment.ConvertAsync(document, todoComments, converted, CancellationToken.None);

            var expectedLists = hostDocument.SelectedSpans;
            Assert.Equal(converted.Count, expectedLists.Count);

            var sourceText = await document.GetTextAsync();
            var tree = await document.GetSyntaxTreeAsync();
            for (var i = 0; i < converted.Count; i++)
            {
                var todo = converted[i];
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
