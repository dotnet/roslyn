// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using static Microsoft.CodeAnalysis.BraceCompletion.AbstractBraceCompletionService;

namespace Microsoft.CodeAnalysis.AutomaticCompletion;

[Export(typeof(IBraceCompletionSessionProvider))]
[ContentType(ContentTypeNames.RoslynContentType)]
[BracePair(CurlyBrace.OpenCharacter, CurlyBrace.CloseCharacter)]
[BracePair(Bracket.OpenCharacter, Bracket.CloseCharacter)]
[BracePair(SingleQuote.OpenCharacter, SingleQuote.CloseCharacter)]
[BracePair(DoubleQuote.OpenCharacter, DoubleQuote.CloseCharacter)]
[BracePair(Parenthesis.OpenCharacter, Parenthesis.CloseCharacter)]
[BracePair(LessAndGreaterThan.OpenCharacter, LessAndGreaterThan.CloseCharacter)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal partial class BraceCompletionSessionProvider(
    IThreadingContext threadingContext,
    ITextBufferUndoManagerProvider undoManager,
    IEditorOperationsFactoryService editorOperationsFactoryService,
    EditorOptionsService editorOptionsService) : IBraceCompletionSessionProvider
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly ITextBufferUndoManagerProvider _undoManager = undoManager;
    private readonly IEditorOperationsFactoryService _editorOperationsFactoryService = editorOperationsFactoryService;
    private readonly EditorOptionsService _editorOptionsService = editorOptionsService;

    public bool TryCreateSession(ITextView textView, SnapshotPoint openingPoint, char openingBrace, char closingBrace, out IBraceCompletionSession session)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        var textSnapshot = openingPoint.Snapshot;
        var document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document != null)
        {
            var editorSessionFactory = document.GetLanguageService<IBraceCompletionServiceFactory>();
            if (editorSessionFactory != null)
            {
                // Brace completion is (currently) not cancellable.
                var cancellationToken = CancellationToken.None;

                var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
                var editorSession = editorSessionFactory.TryGetService(parsedDocument, openingPoint, openingBrace, cancellationToken);
                if (editorSession != null)
                {
                    var undoHistory = _undoManager.GetTextBufferUndoManager(textView.TextBuffer).TextBufferUndoHistory;
                    session = new BraceCompletionSession(
                        textView, openingPoint.Snapshot.TextBuffer, openingPoint, openingBrace, closingBrace,
                        undoHistory, _editorOperationsFactoryService, _editorOptionsService,
                        editorSession, _threadingContext);
                    return true;
                }
            }
        }

        session = null;
        return false;
    }
}
