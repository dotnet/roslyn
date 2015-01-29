// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion.Sessions
{
    internal class LessAndGreaterThanCompletionSession : AbstractTokenBraceCompletionSession
    {
        public LessAndGreaterThanCompletionSession(ISyntaxFactsService syntaxFactsService)
            : base(syntaxFactsService, (int)SyntaxKind.LessThanToken, (int)SyntaxKind.GreaterThanToken)
        {
        }

        public override bool CheckOpeningPoint(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var snapshot = session.SubjectBuffer.CurrentSnapshot;
            var position = session.OpeningPoint.GetPosition(snapshot);
            var token = snapshot.FindToken(position, cancellationToken);

            // check what parser thinks about the newly typed "<" and only proceed if parser thinks it is "<" of 
            // type argument or parameter list
            if (!token.CheckParent<TypeParameterListSyntax>(n => n.LessThanToken == token) &&
                !token.CheckParent<TypeArgumentListSyntax>(n => n.LessThanToken == token) &&
                !PossibleTypeArgument(snapshot, token, cancellationToken))
            {
                return false;
            }

            return true;
        }

        private bool PossibleTypeArgument(ITextSnapshot snapshot, SyntaxToken token, CancellationToken cancellationToken)
        {
            var node = token.Parent as BinaryExpressionSyntax;

            // type argument can be easily ambiguous with normal < operations
            if (node == null || node.Kind() != SyntaxKind.LessThanExpression || node.OperatorToken != token)
            {
                return false;
            }

            // use binding to see whether it is actually generic type or method 
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var model = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var info = model.GetSymbolInfo(node.Left, cancellationToken);

            return info.CandidateSymbols.Any(IsGenericTypeOrMethod);
        }

        private static bool IsGenericTypeOrMethod(ISymbol symbol)
        {
            return symbol.GetArity() > 0;
        }

        public override bool AllowOverType(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            return CheckCurrentPosition(session, cancellationToken);
        }
    }
}
