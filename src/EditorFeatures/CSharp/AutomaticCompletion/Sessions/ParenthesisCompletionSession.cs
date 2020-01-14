// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.VisualStudio.Text.BraceCompletion;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion.Sessions
{
    internal class ParenthesisCompletionSession : AbstractTokenBraceCompletionSession
    {
        public ParenthesisCompletionSession(ISyntaxFactsService syntaxFactsService)
            : base(syntaxFactsService, (int)SyntaxKind.OpenParenToken, (int)SyntaxKind.CloseParenToken)
        {
        }

        public override bool CheckOpeningPoint(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var snapshot = session.SubjectBuffer.CurrentSnapshot;
            var position = session.OpeningPoint.GetPosition(snapshot);
            var token = snapshot.FindToken(position, cancellationToken);

            // check token at the opening point first
            if (!IsValidToken(token) ||
                token.RawKind != OpeningTokenKind ||
                token.SpanStart != position || token.Parent == null)
            {
                return false;
            }

            // now check whether parser think whether there is already counterpart closing parenthesis
            var (openBrace, closeBrace) = token.Parent.GetParentheses();

            // if pair is on the same line, then the closing parenthesis must belong to other tracker.
            // let it through
            if (snapshot.GetLineNumberFromPosition(openBrace.SpanStart) == snapshot.GetLineNumberFromPosition(closeBrace.Span.End))
            {
                return true;
            }

            return (int)closeBrace.Kind() != ClosingTokenKind || closeBrace.Span.Length == 0;
        }
    }
}
