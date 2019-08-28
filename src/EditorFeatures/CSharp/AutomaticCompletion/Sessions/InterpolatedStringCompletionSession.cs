// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion.Sessions
{
    internal class InterpolatedStringCompletionSession : IEditorBraceCompletionSession
    {
        public InterpolatedStringCompletionSession()
        {
        }

        public void AfterReturn(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
        }

        public void AfterStart(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
        }

        public bool AllowOverType(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            return true;
        }

        public bool CheckOpeningPoint(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var snapshot = session.SubjectBuffer.CurrentSnapshot;
            var position = session.OpeningPoint.GetPosition(snapshot);
            var token = snapshot.FindToken(position, cancellationToken);

            return token.IsKind(SyntaxKind.InterpolatedStringStartToken, SyntaxKind.InterpolatedVerbatimStringStartToken)
                && token.Span.End - 1 == position;
        }

        public static bool IsContext(Document document, int position, CancellationToken cancellationToken)
        {
            // Check to see if we're to the right of an $ or an @$
            var text = document.GetTextSynchronously(cancellationToken);

            var start = position - 1;
            if (start < 0)
            {
                return false;
            }

            if (text[start] == '@')
            {
                start--;

                if (start < 0)
                {
                    return false;
                }
            }

            if (text[start] != '$')
            {
                return false;
            }

            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var token = root.FindTokenOnLeftOfPosition(start);

            return root.SyntaxTree.IsExpressionContext(start, token, attributes: false, cancellationToken: cancellationToken)
                || root.SyntaxTree.IsStatementContext(start, token, cancellationToken);
        }
    }
}
