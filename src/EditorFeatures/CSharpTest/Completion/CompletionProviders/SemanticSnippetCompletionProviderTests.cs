// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers.Snippets;
using Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;
using CommitBehavior = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CommitBehavior;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class SemanticSnippetCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    public SemanticSnippetCompletionProviderTests()
    {
        ShowNewSnippetExperience = true;
    }

    internal override Type GetCompletionProviderType()
        => typeof(CSharpSnippetCompletionProvider);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/77868")]
    public async Task CommitIfSnippetWithOpeningParenthesis()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("class C { void M() { if$$ } }", openDocuments: true);
        var testDocument = workspace.Documents.Single();
        var document = workspace.CurrentSolution.GetRequiredDocument(testDocument.Id);
        var view = testDocument.GetTextView();
        var snapshot = view.TextBuffer.CurrentSnapshot;
        var position = testDocument.CursorPosition!.Value;

        var service = document.GetRequiredLanguageService<CompletionService>();
        var list = await service.GetCompletionsAsync(
            document, position, CompletionOptions.Default, document.Project.Solution.Options, CompletionTrigger.Invoke);
        var snippet = list.ItemsList.Single(item => item.DisplayText == "if" && SnippetCompletionItem.IsSnippet(item));
        var item = new VSCompletionItem(
            snippet.DisplayText, Mock.Of<IAsyncCompletionSource>(), new ImageElement(default, "if"), [], "", "if", "if", "if", "if", []);
        CompletionItemData.AddData(item, snippet, new SnapshotPoint(snapshot, position));

        var session = new Mock<IAsyncCompletionSession>();
        session.SetupGet(s => s.TextView).Returns(view);
        session.SetupGet(s => s.Properties).Returns(new PropertyCollection());
        session.SetupGet(s => s.ApplicableToSpan).Returns(snapshot.CreateTrackingSpan(list.Span.Start, list.Span.Length, SpanTrackingMode.EdgeInclusive));
        CompletionSessionData.GetOrCreateSessionData(session.Object).CompletionListSpan = list.Span;

        var expander = new Mock<ILanguageServerSnippetExpander>();
        expander.Setup(e => e.TryExpand(It.IsAny<string>(), It.IsAny<SnapshotSpan>(), view)).Returns(true);
        var manager = new CommitManager(view, workspace.GetService<RecentItemsManager>(), workspace.GlobalOptions,
            workspace.GetService<IThreadingContext>(), expander.Object);
        var result = manager.TryCommit(session.Object, view.TextBuffer, item, '(', CancellationToken.None);

        expander.Verify(e => e.TryExpand(It.IsAny<string>(), It.IsAny<SnapshotSpan>(), view), Times.Once());
        Assert.Equal(CommitBehavior.SuppressFurtherTypeCharCommandHandlers, result.Behavior);
    }

    [WpfFact]
    public Task InsertConsoleSnippetWithInvocationBeforeAndAfterCursorTest()
        => VerifyCustomCommitProviderAsync("""
            class Program
            {
                public void Method()
                {
                    Wr$$Blah
                }
            }
            """, "cw", """
            using System;

            class Program
            {
                public void Method()
                {
                    Console.WriteLine($$);
                }
            }
            """);

    [WpfFact]
    public Task InsertConsoleSnippetWithInvocationUnderscoreBeforeAndAfterCursorTest()
        => VerifyCustomCommitProviderAsync("""
            class Program
            {
                public void Method()
                {
                    _Wr$$Blah_
                }
            }
            """, "cw", """
            using System;

            class Program
            {
                public void Method()
                {
                    Console.WriteLine($$);
                }
            }
            """);
}
