// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Moq;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.PasteTracking
{
    internal sealed class PasteTrackingTestState : IDisposable
    {
        public TestWorkspace Workspace { get; }
        public TestHostDocument HostDocument { get; }

        private readonly IWpfTextView _textView;
        private readonly PasteTrackingService _pasteTrackingService;
        private readonly PasteTrackingPasteCommandHandler _pasteCommandHandler;

        public static PasteTrackingTestState Create(
            string markup,
            string languageName)
        {
            var xml = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document>{1}</Document>
    </Project>
</Workspace>", languageName, markup);

            return CreateFromWorkspaceXml(xml, languageName);
        }

        public static PasteTrackingTestState CreateFromWorkspaceXml(
            string workspaceXml,
            string languageName)
        {

            var workspace = TestWorkspace.Create(
                workspaceXml,
                exportProvider: EditorServicesUtil.ExportProvider);

            return new PasteTrackingTestState(workspace, languageName);
        }

        public PasteTrackingTestState(
            TestWorkspace workspace,
            string languageName)
        {
            Workspace = workspace;
            HostDocument = Workspace.Documents.First();

            _textView = HostDocument.GetTextView();
            _textView.Caret.MoveTo(new SnapshotPoint(_textView.TextSnapshot, HostDocument.CursorPosition.Value));

            // Mock the behavior of running on the UI thread
            var mockThreadingContext = new Mock<IThreadingContext>();
            mockThreadingContext.SetupGet(x => x.HasMainThread).Returns(true);

            _pasteTrackingService = new PasteTrackingService(mockThreadingContext.Object);
            _pasteCommandHandler = new PasteTrackingPasteCommandHandler(_pasteTrackingService);

            if (languageName != LanguageNames.CSharp
                && languageName != LanguageNames.VisualBasic)
            {
                throw new ArgumentException("Invalid language name: " + languageName, nameof(languageName));
            }
        }

        public void SendPaste(string pastedText)
        {
            // Mock the behavior of the `nextCommandHandler` applying the paste
            _pasteCommandHandler.ExecuteCommand(new PasteCommandArgs(_textView, _textView.TextBuffer), ApplyPaste, TestCommandExecutionContext.Create());

            return;

            void ApplyPaste()
            {
                InsertText(pastedText);
            }
        }

        public void InsertText(string instertedText)
        {
            foreach (var selectionSpan in _textView.Selection.SelectedSpans)
            {
                _textView.TextBuffer.Replace(selectionSpan.Span, instertedText);
            }
        }

        public void CloseView()
        {
            _textView.Close();
        }

        /// <summary>
        /// Optionally pass in a TextSpan to assert is equal to the pasted text span 
        /// </summary>
        public void AssertHasPastedTextSpan(TextSpan textSpan = default)
        {
            var document = Workspace.CurrentSolution.GetDocument(HostDocument.Id);
            Assert.True(_pasteTrackingService.TryGetPastedTextSpan(document, out var pastedTextSpan));

            if (textSpan.IsEmpty)
            {
                return;
            }

            Assert.Equal(textSpan, pastedTextSpan);
        }

        public void AssertMissingPastedTextSpan()
        {
            var document = Workspace.CurrentSolution.GetDocument(HostDocument.Id);
            Assert.False(_pasteTrackingService.TryGetPastedTextSpan(document, out var _));
        }

        public void Dispose()
        {
            Workspace.Dispose();
        }
    }
}
