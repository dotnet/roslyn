// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
{
    [Export(typeof(IBraceCompletionSessionProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [BracePair(CurlyBrace.OpenCharacter, CurlyBrace.CloseCharacter)]
    [BracePair(Bracket.OpenCharacter, Bracket.CloseCharacter)]
    [BracePair(SingleQuote.OpenCharacter, SingleQuote.CloseCharacter)]
    [BracePair(DoubleQuote.OpenCharacter, DoubleQuote.CloseCharacter)]
    [BracePair(Parenthesis.OpenCharacter, Parenthesis.CloseCharacter)]
    [BracePair(LessAndGreaterThan.OpenCharacter, LessAndGreaterThan.CloseCharacter)]
    internal partial class BraceCompletionSessionProvider : ForegroundThreadAffinitizedObject, IBraceCompletionSessionProvider
    {
        private readonly ITextBufferUndoManagerProvider _undoManager;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BraceCompletionSessionProvider(
            IThreadingContext threadingContext,
            ITextBufferUndoManagerProvider undoManager,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(threadingContext)
        {
            _undoManager = undoManager;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public bool TryCreateSession(ITextView textView, SnapshotPoint openingPoint, char openingBrace, char closingBrace, out IBraceCompletionSession session)
        {
            this.AssertIsForeground();

            var textSnapshot = openingPoint.Snapshot;
            var document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var editorSessionFactory = document.GetLanguageService<IEditorBraceCompletionSessionFactory>();
                if (editorSessionFactory != null)
                {
                    // Brace completion is (currently) not cancellable.
                    var cancellationToken = CancellationToken.None;

                    var editorSession = editorSessionFactory.TryCreateSession(document, openingPoint, openingBrace, cancellationToken);
                    if (editorSession != null)
                    {
                        var undoHistory = _undoManager.GetTextBufferUndoManager(textView.TextBuffer).TextBufferUndoHistory;
                        session = new BraceCompletionSession(
                            textView, openingPoint.Snapshot.TextBuffer, openingPoint, openingBrace, closingBrace,
                            undoHistory, _editorOperationsFactoryService,
                            editorSession);
                        return true;
                    }
                }
            }

            session = null;
            return false;
        }

        public static class CurlyBrace
        {
            public const char OpenCharacter = '{';
            public const char CloseCharacter = '}';
        }

        public static class Parenthesis
        {
            public const char OpenCharacter = '(';
            public const char CloseCharacter = ')';
        }

        public static class Bracket
        {
            public const char OpenCharacter = '[';
            public const char CloseCharacter = ']';
        }

        public static class LessAndGreaterThan
        {
            public const char OpenCharacter = '<';
            public const char CloseCharacter = '>';
        }

        public static class DoubleQuote
        {
            public const char OpenCharacter = '"';
            public const char CloseCharacter = '"';
        }

        public static class SingleQuote
        {
            public const char OpenCharacter = '\'';
            public const char CloseCharacter = '\'';
        }
    }
}
