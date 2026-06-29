// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable VSPREVIEW_INLINEPROMPT // InlinePromptServiceBase is [Experimental]

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.InlinePrompts;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentationComments;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.DocumentationComments)]
public sealed class CopilotGenerateDocumentationInlinePromptTests
{
    private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeatures
        .AddParts(typeof(TestInlinePromptService), typeof(TestCopilotCodeAnalysisService), typeof(TestCopilotOptionsService));

    [WpfFact]
    public async Task InlinePromptAccept_GeneratesAndAppliesRealDocumentation()
    {
        // A documentation-comment shell already sits above the member, exactly as it does once the user has typed
        // '///' and the snippet has been applied to the buffer.
        const string code = """
            class C
            {
                /// <summary></summary>
                /// <param name="a"></param>
                /// <returns></returns>
                int M(int a) => a;
            }
            """;

        using var workspace = EditorTestWorkspace.CreateCSharp(code, composition: s_composition);
        var testDocument = workspace.Documents.Single();
        var buffer = testDocument.GetTextBuffer();
        var view = testDocument.GetTextView();

        var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        Assert.NotNull(document);

        var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

        var snapshot = buffer.CurrentSnapshot;
        var text = snapshot.GetText();

        // The snippet's SnippetText is the doc-comment block; Position is where that block begins in the buffer so
        // the parsed tag offsets resolve to absolute buffer positions (matching the real command-handler flow).
        var blockStart = text.IndexOf("/// <summary>", StringComparison.Ordinal);
        var blockEnd = text.IndexOf("</returns>", StringComparison.Ordinal) + "</returns>".Length;
        var snippetText = text.Substring(blockStart, blockEnd - blockStart);
        var caretOffset = snippetText.IndexOf("</summary>", StringComparison.Ordinal);

        var snippet = new DocumentationCommentSnippet(
            spanToReplace: new TextSpan(blockStart, blockEnd - blockStart),
            snippetText: snippetText,
            caretOffset: caretOffset,
            position: blockStart,
            memberNode: method,
            indentText: "    ");

        var copilot = (TestCopilotCodeAnalysisService)document.GetRequiredLanguageService<ICopilotCodeAnalysisService>();
        copilot.Documentation = new Dictionary<string, string>
        {
            ["Summary"] = "Adds the supplied value.",
            ["Param-a"] = "the value to add",
            ["Returns"] = "the resulting value",
        };

        var threadingContext = workspace.GetService<IThreadingContext>();
        var manager = workspace.ExportProvider.GetExportedValue<CopilotGenerateDocumentationCommentManager>();
        var inlinePrompt = (TestInlinePromptService)workspace.ExportProvider.GetExportedValue<InlinePromptServiceBase>();
        var waiter = workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>()
            .GetWaiter(FeatureAttribute.GenerateDocumentation);

        // Drive the feature exactly as the command handler does after the '///' shell is applied.
        manager.TriggerDocumentationCommentProposalGeneration(
            document, snippet, snapshot, new VirtualSnapshotPoint(snapshot, blockStart), view, CancellationToken.None);

        // Showing the chip is gated on an async availability/option check, so wait for that operation to finish
        // before asserting the chip was shown.
        await waiter.ExpeditedWaitAsync();

        // The chip was shown with the documentation-generation provider, and accepting it ran the real generation.
        Assert.NotNull(inlinePrompt.CapturedOptions);
        Assert.Equal("Roslyn.GenerateDocumentation", inlinePrompt.CapturedOptions!.ProviderName);
        Assert.Equal("generate documentation", inlinePrompt.CapturedOptions.AcceptDescription);

        Assert.NotNull(inlinePrompt.AcceptTask);
        await inlinePrompt.AcceptTask!;

        // The accept callback completes once generation is done, but the buffer write is intentionally deferred
        // past the callback (so it lands after the InlinePrompt session tears down). Wait for that fire-and-forget
        // operation -- tracked on the GenerateDocumentation async listener -- before asserting on the buffer.
        await waiter.ExpeditedWaitAsync();

        var result = buffer.CurrentSnapshot.GetText();
        Assert.Contains("<summary>Adds the supplied value.</summary>", result);
        Assert.Contains("the value to add", result);
        Assert.Contains("the resulting value", result);
    }

    [WpfFact]
    public async Task InlinePrompt_NotShown_WhenGenerateDocumentationOptionDisabled()
    {
        // Same starting point as the accept test -- a doc-comment shell above the member -- but with the
        // "generate documentation comment" option turned off. The chip must not be offered.
        const string code = """
            class C
            {
                /// <summary></summary>
                /// <param name="a"></param>
                /// <returns></returns>
                int M(int a) => a;
            }
            """;

        using var workspace = EditorTestWorkspace.CreateCSharp(code, composition: s_composition);
        var testDocument = workspace.Documents.Single();
        var buffer = testDocument.GetTextBuffer();
        var view = testDocument.GetTextView();

        var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        Assert.NotNull(document);

        var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

        var snapshot = buffer.CurrentSnapshot;
        var text = snapshot.GetText();
        var blockStart = text.IndexOf("/// <summary>", StringComparison.Ordinal);
        var blockEnd = text.IndexOf("</returns>", StringComparison.Ordinal) + "</returns>".Length;
        var snippetText = text.Substring(blockStart, blockEnd - blockStart);
        var caretOffset = snippetText.IndexOf("</summary>", StringComparison.Ordinal);

        var snippet = new DocumentationCommentSnippet(
            spanToReplace: new TextSpan(blockStart, blockEnd - blockStart),
            snippetText: snippetText,
            caretOffset: caretOffset,
            position: blockStart,
            memberNode: method,
            indentText: "    ");

        // Disable the option that gates the feature.
        var options = (TestCopilotOptionsService)document.GetRequiredLanguageService<ICopilotOptionsService>();
        options.GenerateDocumentationCommentEnabled = false;

        var manager = workspace.ExportProvider.GetExportedValue<CopilotGenerateDocumentationCommentManager>();
        var inlinePrompt = (TestInlinePromptService)workspace.ExportProvider.GetExportedValue<InlinePromptServiceBase>();
        var waiter = workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>()
            .GetWaiter(FeatureAttribute.GenerateDocumentation);

        manager.TriggerDocumentationCommentProposalGeneration(
            document, snippet, snapshot, new VirtualSnapshotPoint(snapshot, blockStart), view, CancellationToken.None);

        await waiter.ExpeditedWaitAsync();

        // The chip was never shown, so no options were captured and no accept gesture was offered.
        Assert.Null(inlinePrompt.CapturedOptions);
        Assert.Null(inlinePrompt.AcceptTask);
    }

    [System.ComponentModel.Composition.Export(typeof(InlinePromptServiceBase))]
    private sealed class TestInlinePromptService : InlinePromptServiceBase
    {
        public InlinePromptOptions? CapturedOptions;
        public Task? AcceptTask;

        [System.ComponentModel.Composition.ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestInlinePromptService()
        {
        }

        public override IDisposable? Show(
            ITextView view, VirtualSnapshotPoint position, Func<CancellationToken, Task> onAcceptAsync, InlinePromptOptions options)
        {
            CapturedOptions = options;
            // Simulate the user immediately accepting the chip.
            AcceptTask = onAcceptAsync(CancellationToken.None);
            return new NoOpDisposable();
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    [ExportLanguageService(typeof(ICopilotOptionsService), LanguageNames.CSharp), Shared, PartNotDiscoverable]
    private sealed class TestCopilotOptionsService : ICopilotOptionsService
    {
        public bool GenerateDocumentationCommentEnabled = true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestCopilotOptionsService() { }

        public Task<bool> IsRefineOptionEnabledAsync() => Task.FromResult(false);
        public Task<bool> IsCodeAnalysisOptionEnabledAsync() => Task.FromResult(false);
        public Task<bool> IsOnTheFlyDocsOptionEnabledAsync() => Task.FromResult(false);
        public Task<bool> IsGenerateDocumentationCommentOptionEnabledAsync() => Task.FromResult(GenerateDocumentationCommentEnabled);
        public Task<bool> IsImplementNotImplementedExceptionEnabledAsync() => Task.FromResult(false);
    }

    [ExportLanguageService(typeof(ICopilotCodeAnalysisService), LanguageNames.CSharp), Shared, PartNotDiscoverable]
    private sealed class TestCopilotCodeAnalysisService : ICopilotCodeAnalysisService
    {
        public Dictionary<string, string>? Documentation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestCopilotCodeAnalysisService() { }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> IsFileExcludedAsync(string filePath, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<(Dictionary<string, string>? responseDictionary, bool isQuotaExceeded)> GetDocumentationCommentAsync(DocumentationCommentProposal proposal, CancellationToken cancellationToken)
            => Task.FromResult<(Dictionary<string, string>?, bool)>((Documentation, false));

        public Task<ImmutableArray<string>> GetAvailablePromptTitlesAsync(Document document, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AnalyzeDocumentAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ImmutableArray<Diagnostic>> GetCachedDocumentDiagnosticsAsync(Document document, TextSpan? span, ImmutableArray<string> promptTitles, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<string> GetOnTheFlyDocsPromptAsync(OnTheFlyDocsInfo onTheFlyDocsInfo, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<(string responseString, bool isQuotaExceeded)> GetOnTheFlyDocsResponseAsync(string prompt, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> IsImplementNotImplementedExceptionsAvailableAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ImmutableDictionary<SyntaxNode, ImplementationDetails>> ImplementNotImplementedExceptionsAsync(Document document, ImmutableDictionary<SyntaxNode, ImmutableArray<ReferencedSymbol>> methodOrProperties, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
