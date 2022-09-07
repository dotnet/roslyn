// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.TodoComments;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.TodoComments
{
    public abstract class AbstractTodoCommentTests
    {
        private static readonly TestComposition s_inProcessComposition = EditorTestCompositions.EditorFeatures;
        private static readonly TestComposition s_outOffProcessComposition = s_inProcessComposition.WithTestHostParts(TestHost.OutOfProcess);

        protected TestWorkspace CreateWorkspace(string codeWithMarker, TestHost host)
            => CreateWorkspace(codeWithMarker, host == TestHost.OutOfProcess ? s_outOffProcessComposition : s_inProcessComposition);

        protected abstract TestWorkspace CreateWorkspace(string codeWithMarker, TestComposition testComposition);

        protected async Task TestAsync(string codeWithMarker, TestHost host)
        {
            using var workspace = CreateWorkspace(codeWithMarker, host);

            var tokenList = TodoCommentOptions.Default.TokenList;
            workspace.GlobalOptions.SetGlobalOption(new OptionKey(TodoCommentOptionsStorage.TokenList), tokenList);

            var hostDocument = workspace.Documents.First();
            var initialTextSnapshot = hostDocument.GetTextBuffer().CurrentSnapshot;
            var documentId = hostDocument.Id;

            var document = workspace.CurrentSolution.GetRequiredDocument(documentId);
            var service = document.GetRequiredLanguageService<ITodoCommentDataService>();
            var todoComments = await service.GetTodoCommentDataAsync(document, TodoCommentDescriptor.Parse(tokenList), CancellationToken.None);

            var expectedLists = hostDocument.SelectedSpans;
            Assert.Equal(todoComments.Length, expectedLists.Count);

            var sourceText = await document.GetTextAsync();
            var tree = await document.GetSyntaxTreeAsync();
            for (var i = 0; i < todoComments.Length; i++)
            {
                var todo = todoComments[i];
                var span = expectedLists[i];

                var line = initialTextSnapshot.GetLineFromPosition(span.Start);
                var text = initialTextSnapshot.GetText(span.ToSpan());

                Assert.Equal(todo.MappedSpan.Span.Start.Line, line.LineNumber);
                Assert.Equal(todo.MappedSpan.Span.Start.Character, span.Start - line.Start);
                Assert.Equal(todo.Message, text);
            }
        }
    }
}
