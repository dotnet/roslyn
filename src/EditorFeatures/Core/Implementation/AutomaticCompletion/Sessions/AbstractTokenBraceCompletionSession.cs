// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions
{
    internal abstract class AbstractTokenBraceCompletionSession : IEditorBraceCompletionSession
    {
        private readonly ISyntaxFactsService _syntaxFactsService;

        protected int OpeningTokenKind { get; }
        protected int ClosingTokenKind { get; }

        protected AbstractTokenBraceCompletionSession(
            ISyntaxFactsService syntaxFactsService,
            int openingTokenKind,
            int closingTokenKind)
        {
            _syntaxFactsService = syntaxFactsService;
            this.OpeningTokenKind = openingTokenKind;
            this.ClosingTokenKind = closingTokenKind;
        }

        public virtual bool CheckOpeningPoint(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var snapshot = session.SubjectBuffer.CurrentSnapshot;
            var position = session.OpeningPoint.GetPosition(snapshot);
            var token = snapshot.FindToken(position, cancellationToken);

            if (!IsValidToken(token))
            {
                return false;
            }

            return token.RawKind == OpeningTokenKind && token.SpanStart == position;
        }

        protected bool IsValidToken(SyntaxToken token)
            => token.Parent != null && !_syntaxFactsService.IsSkippedTokensTrivia(token.Parent);

        public virtual void AfterStart(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
        }

        public virtual void AfterReturn(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
        }

        public virtual bool AllowOverType(IBraceCompletionSession session, CancellationToken cancellationToken)
            => CheckCurrentPosition(session, cancellationToken) && CheckClosingTokenKind(session, cancellationToken);

        protected bool CheckClosingTokenKind(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var document = session.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var root = document.GetSyntaxRootSynchronously(cancellationToken);
                var position = session.ClosingPoint.GetPosition(session.SubjectBuffer.CurrentSnapshot);

                return root.FindTokenFromEnd(position, includeZeroWidth: false, findInsideTrivia: true).RawKind == this.ClosingTokenKind;
            }

            return true;
        }

        protected bool CheckCurrentPosition(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var document = session.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                // make sure auto closing is called from a valid position
                var tree = document.GetSyntaxRootSynchronously(cancellationToken).SyntaxTree;

                return !_syntaxFactsService.IsInNonUserCode(tree, session.GetCaretPosition().Value, cancellationToken);
            }

            return true;
        }
    }
}
