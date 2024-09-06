// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking;

internal sealed class RenameTrackingTestState : IDisposable
{
    private readonly ITagger<RenameTrackingTag> _tagger;
    public readonly EditorTestWorkspace Workspace;
    private readonly IWpfTextView _view;
    private readonly ITextUndoHistoryRegistry _historyRegistry;
    private string _notificationMessage = null;

    private readonly EditorTestHostDocument _hostDocument;
    public EditorTestHostDocument HostDocument { get { return _hostDocument; } }

    private readonly IEditorOperations _editorOperations;
    public IEditorOperations EditorOperations { get { return _editorOperations; } }

    private readonly MockRefactorNotifyService _mockRefactorNotifyService;
    public MockRefactorNotifyService RefactorNotifyService { get { return _mockRefactorNotifyService; } }

    private readonly RenameTrackingCodeRefactoringProvider _codeRefactoringProvider;
    private readonly RenameTrackingCancellationCommandHandler _commandHandler = new RenameTrackingCancellationCommandHandler();

    public static RenameTrackingTestState Create(
        string markup,
        string languageName,
        bool onBeforeGlobalSymbolRenamedReturnValue = true,
        bool onAfterGlobalSymbolRenamedReturnValue = true)
    {
        var workspace = CreateTestWorkspace(markup, languageName);
        return new RenameTrackingTestState(workspace, languageName, onBeforeGlobalSymbolRenamedReturnValue, onAfterGlobalSymbolRenamedReturnValue);
    }

    public static RenameTrackingTestState CreateFromWorkspaceXml(
        string workspaceXml,
        string languageName,
        bool onBeforeGlobalSymbolRenamedReturnValue = true,
        bool onAfterGlobalSymbolRenamedReturnValue = true)
    {
        var workspace = CreateTestWorkspace(workspaceXml);
        return new RenameTrackingTestState(workspace, languageName, onBeforeGlobalSymbolRenamedReturnValue, onAfterGlobalSymbolRenamedReturnValue);
    }

    public RenameTrackingTestState(
        EditorTestWorkspace workspace,
        string languageName,
        bool onBeforeGlobalSymbolRenamedReturnValue = true,
        bool onAfterGlobalSymbolRenamedReturnValue = true)
    {
        this.Workspace = workspace;

        _hostDocument = Workspace.Documents.First();
        _view = _hostDocument.GetTextView();
        _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, _hostDocument.CursorPosition.Value));
        _editorOperations = Workspace.GetService<IEditorOperationsFactoryService>().GetEditorOperations(_view);
        _historyRegistry = Workspace.ExportProvider.GetExport<ITextUndoHistoryRegistry>().Value;
        _mockRefactorNotifyService = new MockRefactorNotifyService
        {
            OnBeforeSymbolRenamedReturnValue = onBeforeGlobalSymbolRenamedReturnValue,
            OnAfterSymbolRenamedReturnValue = onAfterGlobalSymbolRenamedReturnValue
        };

        // Mock the action taken by the workspace INotificationService
        var notificationService = (INotificationServiceCallback)Workspace.Services.GetRequiredService<INotificationService>();
        var callback = new Action<string, string, NotificationSeverity>((message, title, severity) => _notificationMessage = message);
        notificationService.NotificationCallback = callback;

        var tracker = new RenameTrackingTaggerProvider(
            Workspace.GetService<IThreadingContext>(),
            Workspace.GetService<IInlineRenameService>(),
            Workspace.GetService<IDiagnosticAnalyzerService>(),
            Workspace.GetService<IGlobalOptionService>(),
            Workspace.GetService<IAsynchronousOperationListenerProvider>());

        _tagger = tracker.CreateTagger<RenameTrackingTag>(_hostDocument.GetTextBuffer());

        if (languageName is LanguageNames.CSharp or
            LanguageNames.VisualBasic)
        {
            _codeRefactoringProvider = new RenameTrackingCodeRefactoringProvider(
                _historyRegistry,
                [_mockRefactorNotifyService]);
        }
        else
        {
            throw new ArgumentException("Invalid language name: " + languageName, nameof(languageName));
        }
    }

    private static EditorTestWorkspace CreateTestWorkspace(string code, string languageName)
    {
        return CreateTestWorkspace(string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document>{1}</Document>
    </Project>
</Workspace>", languageName, code));
    }

    private static EditorTestWorkspace CreateTestWorkspace(string xml)
    {
        return EditorTestWorkspace.Create(xml, composition: EditorTestCompositions.EditorFeaturesWpf);
    }

    public void SendEscape()
        => _commandHandler.ExecuteCommand(new EscapeKeyCommandArgs(_view, _view.TextBuffer), TestCommandExecutionContext.Create());

    public void MoveCaret(int delta)
    {
        var position = _view.Caret.Position.BufferPosition.Position;
        _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, position + delta));
    }

    public void Undo(int count = 1)
    {
        var history = _historyRegistry.GetHistory(_view.TextBuffer);
        history.Undo(count);
    }

    public void Redo(int count = 1)
    {
        var history = _historyRegistry.GetHistory(_view.TextBuffer);
        history.Redo(count);
    }

    public async Task AssertNoTag()
    {
        await WaitForAsyncOperationsAsync();

        var tags = _tagger.GetTags(_view.TextBuffer.CurrentSnapshot.GetSnapshotSpanCollection());

        Assert.Equal(0, tags.Count());
    }

    /// <param name="textSpan">If <see langword="null"/> the current caret position will be used.</param>
    public async Task<CodeAction> TryGetCodeActionAsync(TextSpan? textSpan = null)
    {
        var span = textSpan ?? new TextSpan(_view.Caret.Position.BufferPosition, 0);

        var document = this.Workspace.CurrentSolution.GetDocument(_hostDocument.Id);

        var actions = new List<CodeAction>();
        var context = new CodeRefactoringContext(
            document, span, actions.Add, CancellationToken.None);
        await _codeRefactoringProvider.ComputeRefactoringsAsync(context);
        return actions.SingleOrDefault();
    }

    public async Task AssertTag(string expectedFromName, string expectedToName, bool invokeAction = false)
    {
        await WaitForAsyncOperationsAsync();

        var tags = _tagger.GetTags(_view.TextBuffer.CurrentSnapshot.GetSnapshotSpanCollection());

        // There should only ever be one tag
        Assert.Equal(1, tags.Count());

        var tag = tags.Single();

        // There should only be one code action for the tag
        var codeAction = await TryGetCodeActionAsync(tag.Span.Span.ToTextSpan());
        Assert.NotNull(codeAction);
        Assert.Equal(string.Format(EditorFeaturesResources.Rename_0_to_1, expectedFromName, expectedToName), codeAction.Title);

        if (invokeAction)
        {
            var operations = (await codeAction.GetOperationsAsync(CancellationToken.None)).ToArray();
            Assert.Equal(1, operations.Length);

            await operations[0].TryApplyAsync(this.Workspace, this.Workspace.CurrentSolution, CodeAnalysisProgress.None, CancellationToken.None);
        }
    }

    public void AssertNoNotificationMessage()
        => Assert.Null(_notificationMessage);

    public void AssertNotificationMessage()
        => Assert.NotNull(_notificationMessage);

    private async Task WaitForAsyncOperationsAsync()
    {
        var provider = Workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        await provider.WaitAllDispatcherOperationAndTasksAsync(
            Workspace,
            FeatureAttribute.RenameTracking,
            FeatureAttribute.SolutionCrawlerLegacy,
            FeatureAttribute.Workspace,
            FeatureAttribute.EventHookup);
    }

    public void Dispose()
        => Workspace.Dispose();
}
