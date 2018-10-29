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
    /// When user types <c>;</c> while completion list is displayed, completion is committed and parentheses and semi-colon are added
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
            public int position;

            public ClosingDelimeterNeeded(SyntaxKind braceCharacter, bool isMissing, int position)
            {
                this.braceCharacter = braceCharacter;
                this.isMissing = isMissing;
                this.position = position;
            }
        }

        [ImportingConstructor]
        public CompleteStatementCommandHandler(ITextUndoHistoryRegistry undoHistoryRegistry, IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public string DisplayName => "Test"; //TODO add resource

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

            if (!ApplicableToken(token, caret, caretPosition))
            {
                nextCommandHandler();
                return;
            }

            var currentNode = token.Parent;
            if (currentNode == null)
            {
                nextCommandHandler();
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var delimitingClosures = new Queue<ClosingDelimeterNeeded>();
            var countExistingDelimiters = 0;

            // keep going until you reach syntax that requires a semicolon
            while (!ReachedSemicolonSyntax(currentNode, syntaxFacts))
            {
                switch (currentNode.Kind())
                {
                    case SyntaxKind.ArgumentList:
                        var argumentList = (ArgumentListSyntax)currentNode;
                        if (!argumentList.CloseParenToken.IsMissing)
                        {

                            countExistingDelimiters++;
                        }
                        delimitingClosures.Enqueue(new ClosingDelimeterNeeded(SyntaxKind.CloseParenToken, argumentList.CloseParenToken.IsMissing, argumentList.Span.End));
                        break;
                    case SyntaxKind.ParenthesizedExpression:
                        var parenthesizedExpression = (ParenthesizedExpressionSyntax)currentNode;
                        if (!parenthesizedExpression.CloseParenToken.IsMissing)
                        {
                            countExistingDelimiters++;
                        }
                        delimitingClosures.Enqueue(new ClosingDelimeterNeeded(SyntaxKind.CloseParenToken, parenthesizedExpression.CloseParenToken.IsMissing, parenthesizedExpression.Span.End));
                        break;
                    case SyntaxKind.BracketedArgumentList:
                        var bracketedArgumentList = (BracketedArgumentListSyntax)currentNode;
                        if (!bracketedArgumentList.CloseBracketToken.IsMissing)
                        {
                            countExistingDelimiters++;
                        }
                        delimitingClosures.Enqueue(new ClosingDelimeterNeeded(SyntaxKind.CloseBracketToken, bracketedArgumentList.CloseBracketToken.IsMissing, bracketedArgumentList.Span.End));
                        break;
                    case SyntaxKind.ObjectInitializerExpression:
                        var initializerExpressionSyntax = (InitializerExpressionSyntax)currentNode;
                        if (!initializerExpressionSyntax.CloseBraceToken.IsMissing)
                        {
                            countExistingDelimiters++;
                        }
                        delimitingClosures.Enqueue(new ClosingDelimeterNeeded(SyntaxKind.CloseBraceToken, initializerExpressionSyntax.CloseBraceToken.IsMissing, initializerExpressionSyntax.Span.End));
                        break;
                }

                if (currentNode.Parent == null)
                {
                    nextCommandHandler();
                    return;
                }

                currentNode = currentNode.Parent;
            }

            if (delimitingClosures.Count == 0)
            {
                nextCommandHandler();
                return;
            }

            // check closing delimeters, if missing add it, if not skip it
            var peek = delimitingClosures.Peek();
            var currentToken = root.FindToken(currentNode.Span.End);
            var previousToken = currentToken.GetPreviousToken();
            var newCaretPosition = GetEndPosition(root, currentNode.Span.End, currentNode.Kind(), peek.isMissing) - countExistingDelimiters;
            //var newCaretPosition = countExistingDelimiters == 0 ? GetEndPosition(root, currentNode.Span.End, currentNode.Kind()) - countExistingDelimiters : currentNode.Span.End;
            for (int offset = 0; offset <= delimitingClosures.Count; offset++)
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
                    //newCaretPosition = GetNewCaretPosition(root, newCaretPosition+1, c.braceCharacter, offset);
                    newCaretPosition++;
                }

            }

            args.TextView.TryMoveCaretToAndEnsureVisible(args.SubjectBuffer.CurrentSnapshot.GetPoint(newCaretPosition));
            nextCommandHandler();
        }

        private bool ReachedSemicolonSyntax(SyntaxNode currentNode, ISyntaxFactsService syntaxFacts)
        {
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

        private int GetNewCaretPosition(SyntaxNode root, int newCaretPosition, SyntaxKind braceCharacter, int offset)
        {
            var currentToken = root.FindToken(newCaretPosition);
            while (currentToken.Kind() != braceCharacter)
            {
                newCaretPosition++;
                currentToken = root.FindToken(newCaretPosition);
            }
            return newCaretPosition+offset;
        }
        

        private bool SemicolonIsMissing(SyntaxNode currentNode)
        {
            switch (currentNode.Kind())
            {
                case SyntaxKind.LocalDeclarationStatement:
                    return ((LocalDeclarationStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.ReturnStatement:
                    return ((ReturnStatementSyntax)currentNode).SemicolonToken.IsMissing;
                    // need to add all the other statement kinds here, unless there is an easier way?
                default:
                        return false;
            }
        }

        /// <summary>
        /// To account for the new line character at the end of a line, this returns the previous tokens end  
        /// </summary>
        private int GetEndPosition(SyntaxNode root, int end, SyntaxKind nodeKind, bool outerDelimiterMissing)
        {
            // in these cases, the previous token's trivia has end of line trivia that we want to get ahead of
            if (nodeKind == SyntaxKind.VariableDeclaration)
            {
                if (!outerDelimiterMissing)
                {
                    return end;
                }
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
            var caretBeforeToken = caretPosition == token.SpanStart;

            switch (token.Kind())
            {
                case SyntaxKind.BreakKeyword:
                case SyntaxKind.ContinueKeyword:
                case SyntaxKind.EmptyStatement:
                    {
                        return false;
                    }
                case SyntaxKind.IdentifierToken:
                    if (!caretAtEndOfLine && !caretBeforeToken)
                    {
                        return false;
                    }
                    break;
                case SyntaxKind.OpenParenToken:
                    if (!caretAtEndOfLine)
                    {
                        // caret is before OpenParenToken, but are there outer enclosing delimiters to consider?
                        return false;
                    }
                    break;
                case SyntaxKind.InterpolatedStringTextToken:
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.CharacterLiteralToken:
                    if (!caretAtEndOfLine)
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