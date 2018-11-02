// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement
{
    /// <summary>
    /// When user types <c>;</c> in a statement, closing delimiters and semi-colon are added and caret is placed after the semicolon
    /// </summary>
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(CompleteStatementCommandHandler))]
    internal sealed class CompleteStatementCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        internal class ClosingDelimeterNeeded
        {
            public SyntaxKind braceCharacter;
            public bool isMissing;

            public ClosingDelimeterNeeded(SyntaxKind braceCharacter, bool isMissing)
            {
                this.braceCharacter = braceCharacter;
                this.isMissing = isMissing;
            }
        }

        [ImportingConstructor]
        public CompleteStatementCommandHandler(ITextUndoHistoryRegistry undoHistoryRegistry, IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public string DisplayName => CSharpEditorResources.Complete_statement;

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            if (args.TypedChar != ';')
            {
                nextCommandHandler();
                return;
            }

            var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caret.HasValue)
            {
                nextCommandHandler();
                return;
            }

            var document = caret.Value.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                nextCommandHandler();
                return;
            }

            var root = document.GetSyntaxRootSynchronously(executionContext.OperationContext.UserCancellationToken);
            var caretPosition = caret.Value.Position;
            var token = root.FindToken(caretPosition);
            var currentNode = token.Parent;

            if ((token.Kind() == SyntaxKind.OpenBraceToken || token.Kind() == SyntaxKind.OpenBracketToken || token.Kind() == SyntaxKind.OpenParenToken) && token.Span.End == caret.Value.Position)
            {
                currentNode = currentNode.Parent;
            }
            if (currentNode == null)
            {   
                nextCommandHandler();
                return;
            }
            
            if (!ApplicableToken(token, caret, caretPosition))
            {
                nextCommandHandler();
                return;
            }


            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var delimitingClosures = new Queue<ClosingDelimeterNeeded>();
            var numberDelimiterSpacesToSkip = 0;
            var enqueueFinalClosingDelimiter = false;

            // work your way out, enqueueing closing delimiters until you reach statement syntax that requires a semicolon
            // track where the earliest existing delimiter is to use for insertion position later
            var firstPosition = -1;
            while (!ReachedSemicolonSyntax(currentNode, syntaxFacts, ref enqueueFinalClosingDelimiter))
            {
                numberDelimiterSpacesToSkip = enqueueDelimiterIfNeeded(currentNode, delimitingClosures, numberDelimiterSpacesToSkip, ref firstPosition);

                if (currentNode.Parent == null)
                {
                    nextCommandHandler();
                    return;
                }

                currentNode = currentNode.Parent;
            }

            // if the statement syntax itself requires a closing delimeter, enqueue it
            if (enqueueFinalClosingDelimiter)
            {
                numberDelimiterSpacesToSkip = enqueueFinalDelimeter(currentNode, delimitingClosures, numberDelimiterSpacesToSkip);
            }

            // bail if you aren't inside any enclosures
            if (delimitingClosures.Count == 0)
            {
                nextCommandHandler();
                return;
            }

            // check closing delimeters, if missing add it, if not skip it
            var newCaretPosition = GetEndingPosition(root, firstPosition, currentNode, numberDelimiterSpacesToSkip);
            while (delimitingClosures.Count > 0)
            {

                var c = delimitingClosures.Dequeue();
                if (c.isMissing)
                {
                    args.SubjectBuffer.Insert(newCaretPosition, GetInsertionCharacter(c.braceCharacter));
                    newCaretPosition++;
                }
                else
                {
                    // TO DO currently only works if existing delimiter is in next position
                    while (!root.FindToken(newCaretPosition).IsKind(c.braceCharacter))
                    {
                        newCaretPosition++;
                    }
                    newCaretPosition++;
                }

            }

            
            args.TextView.TryMoveCaretToAndEnsureVisible(args.SubjectBuffer.CurrentSnapshot.GetPoint(newCaretPosition));
            //args.TextView.TryMoveCaretToAndEnsureVisible(args.SubjectBuffer.CurrentSnapshot.GetPoint(positionInSubjectBuffer) + offsetForMissingSemicolon - 1);
            nextCommandHandler();
        }

        private int GetEndingPosition(SyntaxNode root, int firstPosition, SyntaxNode currentNode, int numberDelimiterSpacesToSkip)
        {
            int newPosition;
            if (firstPosition == -1)
            {
                newPosition = GetEndPosition(root, currentNode.Span.End, currentNode.Kind());
            }
            else
            {
                newPosition = firstPosition;
            }

            newPosition -= SemiColonIsMissing(currentNode) ? 0 : 1;

            return newPosition-numberDelimiterSpacesToSkip;
        }

        private bool SemiColonIsMissing(SyntaxNode currentNode)
        {
            switch (currentNode.Kind())
            {
                case SyntaxKind.LocalDeclarationStatement:
                    return ((LocalDeclarationStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.ReturnStatement:
                    return ((ReturnStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.VariableDeclaration:
                    return SemiColonIsMissing(currentNode.Parent);
                case SyntaxKind.ThrowStatement:
                    return ((ThrowStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.DoStatement:
                    return ((DoStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                    return ((AccessorDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.FieldDeclaration:
                    return ((FieldDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.ForStatement:
                    return ((ForStatementSyntax)currentNode).FirstSemicolonToken.IsMissing;
                // need to add all the other statement kinds here, unless there is an easier way?
                default:
                    return false;
            }
        }

        private static int enqueueFinalDelimeter(SyntaxNode currentNode, Queue<ClosingDelimeterNeeded> delimitingClosures, int numberDelimiterSpacesToSkip)
        {
            switch (currentNode.Kind())
            {
                case SyntaxKind.DoStatement:
                    var dostatement = (DoStatementSyntax)currentNode;
                    if (!dostatement.CloseParenToken.IsMissing)
                    {
                        numberDelimiterSpacesToSkip++;
                    }
                    delimitingClosures.Enqueue(new ClosingDelimeterNeeded(SyntaxKind.CloseParenToken, dostatement.CloseParenToken.IsMissing));
                    break;
            }

            return numberDelimiterSpacesToSkip;
        }

        private static int enqueueDelimiterIfNeeded(SyntaxNode currentNode, Queue<ClosingDelimeterNeeded> delimitingClosures, int numberDelimiterSpacesToSkip, ref int firstPosition)
        {
            switch (currentNode.Kind())
            {
                case SyntaxKind.ArgumentList:
                    var argumentList = (ArgumentListSyntax)currentNode;
                    if (!argumentList.CloseParenToken.IsMissing)
                    {
                        numberDelimiterSpacesToSkip++;
                        // don't change firstPosition because you might be on an argument that is further up in the arg list and therefore incorrect
                        // i.e., (x.ToString(I), y)
                    }
                    delimitingClosures.Enqueue(new ClosingDelimeterNeeded(SyntaxKind.CloseParenToken, argumentList.CloseParenToken.IsMissing));
                    break;
                case SyntaxKind.ParenthesizedExpression:
                    var parenthesizedExpression = (ParenthesizedExpressionSyntax)currentNode;
                    if (!parenthesizedExpression.CloseParenToken.IsMissing)
                    {
                        numberDelimiterSpacesToSkip++;
                        firstPosition = firstPosition == -1 ? parenthesizedExpression.CloseParenToken.Span.Start : firstPosition;
                    }
                    delimitingClosures.Enqueue(new ClosingDelimeterNeeded(SyntaxKind.CloseParenToken, parenthesizedExpression.CloseParenToken.IsMissing));
                    break;
                case SyntaxKind.BracketedArgumentList:
                    var bracketedArgumentList = (BracketedArgumentListSyntax)currentNode;
                    if (!bracketedArgumentList.CloseBracketToken.IsMissing)
                    {
                        numberDelimiterSpacesToSkip++;
                        firstPosition = firstPosition == -1 ? bracketedArgumentList.CloseBracketToken.Span.Start : firstPosition;
                    }
                    delimitingClosures.Enqueue(new ClosingDelimeterNeeded(SyntaxKind.CloseBracketToken, bracketedArgumentList.CloseBracketToken.IsMissing));
                    break;
                case SyntaxKind.ObjectInitializerExpression:
                    var initializerExpressionSyntax = (InitializerExpressionSyntax)currentNode;
                    if (!initializerExpressionSyntax.CloseBraceToken.IsMissing)
                    {
                        numberDelimiterSpacesToSkip++;
                        firstPosition = firstPosition == -1 ? initializerExpressionSyntax.CloseBraceToken.Span.Start : firstPosition;
                    }
                    delimitingClosures.Enqueue(new ClosingDelimeterNeeded(SyntaxKind.CloseBraceToken, initializerExpressionSyntax.CloseBraceToken.IsMissing));
                    break;
            }

            return numberDelimiterSpacesToSkip;
        }

        private bool ReachedSemicolonSyntax(SyntaxNode currentNode, ISyntaxFactsService syntaxFacts, ref bool enqueueClosingDelimiter)
        {
            enqueueClosingDelimiter = false;

            if (currentNode.IsKind(SyntaxKind.DoStatement))
            {
                enqueueClosingDelimiter = true;
                return true;
            }

            if (syntaxFacts.IsStatement(currentNode)
                || currentNode.IsKind(SyntaxKind.GetAccessorDeclaration)
                || currentNode.IsKind(SyntaxKind.SetAccessorDeclaration)
                || currentNode.IsKind(SyntaxKind.LocalDeclarationStatement))
            {
                return true;
            }

            if (currentNode.IsKind(SyntaxKind.VariableDeclaration)
                && !currentNode.Ancestors().Any(n => n.IsKind(SyntaxKind.LocalDeclarationStatement)))
            {
                return true;
            }

            return false;
        }

        private string GetInsertionCharacter(SyntaxKind braceCharacter)
        {
            switch (braceCharacter)
            {
                case SyntaxKind.CloseParenToken:
                    return ")";
                case SyntaxKind.CloseBraceToken:
                    return "}";
                case SyntaxKind.CloseBracketToken:
                    return "]";
                default:
                    return "";
            }
        }

        /// <summary>
        /// To account for the new line character at the end of a line, this returns the previous tokens end  
        /// </summary>
        private int GetEndPosition(SyntaxNode root, int end, SyntaxKind nodeKind)
        {

            if (nodeKind == SyntaxKind.VariableDeclaration)
            {
                //if (!outerDelimiterMissing)
                //{
                    return end;
                //}
            }

            // If "end" is at the end of a line, the token has trailing end of line trivia.
            // We want to put our cursor before that trivia, so use previous token for placement.
                var previousToken = root.FindToken(end).GetPreviousToken();
                return previousToken.Span.End;
        }

        private bool ApplicableToken(SyntaxToken token, SnapshotPoint? caret, int caretPosition)
        {
            if (caret == null)
            {
                return false;
            }

            var caretAtEndOfLine = caret.Value.Position == caret.Value.GetContainingLine().End;
            var caretBeforeToken = caretPosition <= token.SpanStart;
            var caretAfterToken = caretAtEndOfLine ? true : caretPosition >= token.Span.End;

            switch (token.Kind())
            {
                case SyntaxKind.OpenParenToken:
                    if (!caretAtEndOfLine)
                    {
                        // caret is before OpenParenToken, but are there outer enclosing delimiters to consider?
                        return false;
                    }
                    break;
                case SyntaxKind.BreakKeyword:
                case SyntaxKind.ContinueKeyword:
                case SyntaxKind.EmptyStatement:
                    {
                        return false;
                    }
                case SyntaxKind.IdentifierToken:
                case SyntaxKind.InterpolatedStringTextToken:
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.CharacterLiteralToken:
                    if (!caretBeforeToken && !caretAfterToken)
                    {
                        return false;
                    }
                    break;
            }
            return true;
        }

        public VSCommanding.CommandState GetCommandState(TypeCharCommandArgs args, Func<VSCommanding.CommandState> nextCommandHandler) => nextCommandHandler();

    }
}