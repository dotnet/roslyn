// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions
{
    internal class EditorBraceCompletionSession : IEditorBraceCompletionSession
    {
        private readonly IBraceCompletionService _braceCompletionService;

        public EditorBraceCompletionSession(IBraceCompletionService braceCompletionService)
        {
            _braceCompletionService = braceCompletionService;
        }

        public BraceCompletionResult? GetChangesAfterReturn(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var context = GetBraceCompletionContextFromSession(session);
            if (context == null)
            {
                return null;
            }

            return _braceCompletionService.GetTextChangeAfterReturnAsync(context, supportsVirtualSpace: true, cancellationToken).WaitAndGetResult(cancellationToken);
        }

        public virtual bool AllowOverType(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var context = GetBraceCompletionContextFromSession(session);
            if (context != null)
            {
                return _braceCompletionService.AllowOverTypeAsync(context, cancellationToken).WaitAndGetResult(cancellationToken);
            }

            return true;
        }

        public BraceCompletionResult? GetBraceCompletion(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var context = GetBraceCompletionContextFromSession(session);
            if (context == null)
            {
                return null;
            }

            return _braceCompletionService.GetBraceCompletionAsync(context, cancellationToken).WaitAndGetResult(cancellationToken);
        }

        public BraceCompletionResult? GetChangesAfterCompletion(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var context = GetBraceCompletionContextFromSession(session);
            if (context == null)
            {
                return null;
            }

            return _braceCompletionService.GetTextChangesAfterCompletionAsync(context, cancellationToken).WaitAndGetResult(cancellationToken);
        }

        public bool IsValidForBraceCompletion(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
            => _braceCompletionService.IsValidForBraceCompletionAsync(brace, openingPosition, document, cancellationToken).WaitAndGetResult(cancellationToken);

        private static BraceCompletionContext? GetBraceCompletionContextFromSession(IBraceCompletionSession session)
        {
            var snapshot = session.SubjectBuffer.CurrentSnapshot;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var closingSnapshotPoint = session.ClosingPoint.GetPosition(snapshot);
                var openingSnapshotPoint = session.OpeningPoint.GetPosition(snapshot);
                var caretPosition = session.GetCaretPosition()?.Position;
                return new BraceCompletionContext(document, openingSnapshotPoint, closingSnapshotPoint, caretPosition);
            }

            return null;
        }
    }
}
