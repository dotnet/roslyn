// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.CSharp.RenameTracking;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Editor.VisualBasic.RenameTracking;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking
{
    internal sealed class RenameTrackingTestState : IDisposable
    {
        private readonly ITagger<RenameTrackingTag> _tagger;
        public readonly TestWorkspace Workspace;
        private readonly IWpfTextView _view;
        private readonly ITextUndoHistoryRegistry _historyRegistry;
        private string _notificationMessage = null;

        private readonly TestHostDocument _hostDocument;
        public TestHostDocument HostDocument { get { return _hostDocument; } }

        private readonly IEditorOperations _editorOperations;
        public IEditorOperations EditorOperations { get { return _editorOperations; } }

        private readonly MockRefactorNotifyService _mockRefactorNotifyService;
        public MockRefactorNotifyService RefactorNotifyService { get { return _mockRefactorNotifyService; } }

        private readonly CodeFixProvider _codeFixProvider;
        private readonly RenameTrackingCancellationCommandHandler _commandHandler = new RenameTrackingCancellationCommandHandler();

        public static async Task<RenameTrackingTestState> CreateAsync(
            string markup,
            string languageName,
            bool onBeforeGlobalSymbolRenamedReturnValue = true,
            bool onAfterGlobalSymbolRenamedReturnValue = true)
        {
            var workspace = await CreateTestWorkspaceAsync(markup, languageName, TestExportProvider.CreateExportProviderWithCSharpAndVisualBasic());
            return new RenameTrackingTestState(workspace, languageName, onBeforeGlobalSymbolRenamedReturnValue, onAfterGlobalSymbolRenamedReturnValue);
        }

        public RenameTrackingTestState(
            TestWorkspace workspace,
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

            var optionService = this.Workspace.Services.GetService<IOptionService>();

            // Mock the action taken by the workspace INotificationService
            var notificationService = Workspace.Services.GetService<INotificationService>() as INotificationServiceCallback;
            var callback = new Action<string, string, NotificationSeverity>((message, title, severity) => _notificationMessage = message);
            notificationService.NotificationCallback = callback;

            var tracker = new RenameTrackingTaggerProvider(
                _historyRegistry,
                Workspace.ExportProvider.GetExport<Host.IWaitIndicator>().Value,
                Workspace.ExportProvider.GetExport<IInlineRenameService>().Value,
                Workspace.ExportProvider.GetExport<IDiagnosticAnalyzerService>().Value,
                SpecializedCollections.SingletonEnumerable(_mockRefactorNotifyService),
                Workspace.ExportProvider.GetExports<IAsynchronousOperationListener, FeatureMetadata>());

            _tagger = tracker.CreateTagger<RenameTrackingTag>(_hostDocument.GetTextBuffer());

            if (languageName == LanguageNames.CSharp)
            {
                _codeFixProvider = new CSharpRenameTrackingCodeFixProvider(
                    Workspace.ExportProvider.GetExport<Host.IWaitIndicator>().Value,
                    _historyRegistry,
                    SpecializedCollections.SingletonEnumerable(_mockRefactorNotifyService));
            }
            else if (languageName == LanguageNames.VisualBasic)
            {
                _codeFixProvider = new VisualBasicRenameTrackingCodeFixProvider(
                    Workspace.ExportProvider.GetExport<Host.IWaitIndicator>().Value,
                    _historyRegistry,
                    SpecializedCollections.SingletonEnumerable(_mockRefactorNotifyService));
            }
            else
            {
                throw new ArgumentException("Invalid language name: " + languageName, "languageName");
            }
        }

        private static Task<TestWorkspace> CreateTestWorkspaceAsync(string code, string languageName, ExportProvider exportProvider = null)
        {
            var xml = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document>{1}</Document>
    </Project>
</Workspace>", languageName, code);

            return TestWorkspace.CreateAsync(xml, exportProvider: exportProvider);
        }

        public void SendEscape()
        {
            _commandHandler.ExecuteCommand(new EscapeKeyCommandArgs(_view, _view.TextBuffer), () => { });
        }

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

        public async Task<IList<Diagnostic>> GetDocumentDiagnosticsAsync(Document document = null)
        {
            document = document ?? this.Workspace.CurrentSolution.GetDocument(_hostDocument.Id);
            var analyzer = new RenameTrackingDiagnosticAnalyzer();
            return (await DiagnosticProviderTestUtilities.GetDocumentDiagnosticsAsync(analyzer, document, 
                (await document.GetSyntaxRootAsync()).FullSpan)).ToList();
        }

        public async Task AssertTag(string expectedFromName, string expectedToName, bool invokeAction = false)
        {
            await WaitForAsyncOperationsAsync();

            var tags = _tagger.GetTags(_view.TextBuffer.CurrentSnapshot.GetSnapshotSpanCollection());

            // There should only ever be one tag
            Assert.Equal(1, tags.Count());

            var tag = tags.Single();

            var document = this.Workspace.CurrentSolution.GetDocument(_hostDocument.Id);
            var diagnostics = await GetDocumentDiagnosticsAsync(document);

            // There should be a single rename tracking diagnostic
            Assert.Equal(1, diagnostics.Count);
            Assert.Equal(RenameTrackingDiagnosticAnalyzer.DiagnosticId, diagnostics[0].Id);

            var actions = new List<CodeAction>();
            var context = new CodeFixContext(document, diagnostics[0], (a, d) => actions.Add(a), CancellationToken.None);
            await _codeFixProvider.RegisterCodeFixesAsync(context);

            // There should only be one code action
            Assert.Equal(1, actions.Count);

            Assert.Equal(string.Format(EditorFeaturesResources.RenameTo, expectedFromName, expectedToName), actions[0].Title);

            if (invokeAction)
            {
                var operations = (await actions[0].GetOperationsAsync(CancellationToken.None)).ToArray();
                Assert.Equal(1, operations.Length);

                operations[0].Apply(this.Workspace, CancellationToken.None);
            }
        }

        public void AssertNoNotificationMessage()
        {
            Assert.Null(_notificationMessage);
        }

        public void AssertNotificationMessage()
        {
            Assert.NotNull(_notificationMessage);
        }

        private async Task WaitForAsyncOperationsAsync()
        {
            var waiters = Workspace.ExportProvider.GetExportedValues<IAsynchronousOperationWaiter>();
            await waiters.WaitAllAsync();
        }

        public void Dispose()
        {
            Workspace.Dispose();
        }
    }
}
