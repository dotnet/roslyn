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
            

            var token = GetToken(root, caretPosition, caret);
            if (!ApplicableToken(token, caret, caretPosition))
            {
                nextCommandHandler();
                return;
            }

            var currentNode = token.Parent;
            // if cursor is right before a closing delimiter, make sure you start with node outside of delimiters
            if ((token.Kind() == SyntaxKind.OpenBraceToken || token.Kind() == SyntaxKind.OpenBracketToken || token.Kind() == SyntaxKind.OpenParenToken) && token.Span.End == caret.Value.Position)
            {
                currentNode = currentNode.Parent;
            }

            if (currentNode == null)
            {
                nextCommandHandler();
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var lastDelimiterPosition = -1;
            var finalDelimiterNeedsSemicolon = false;

            // work your way out, verifying all delimeters exist until you reach statement syntax that requires a semicolon
            while (!ReachedSemicolonSyntax(currentNode, syntaxFacts, ref finalDelimiterNeedsSemicolon))
            {
                if (!ClosingDelimiterExistsIfNeeded(currentNode, ref lastDelimiterPosition))
                {
                    nextCommandHandler();
                    return;
                }

                if (currentNode.Parent == null)
                {
                    nextCommandHandler();
                    return;
                }

                currentNode = currentNode.Parent;
            }

            if (currentNode.Ancestors().Any(n => n.Kind() == SyntaxKind.ForStatement))
            {
                nextCommandHandler();
                return;
            }

            // if the statement syntax itself requires a closing delimeter, verify it is there
            if (finalDelimiterNeedsSemicolon)
            {
                if (!StatementClosingDelimiterExists(currentNode, ref lastDelimiterPosition))
                {
                    nextCommandHandler();
                    return;
                }
            }

            // if you haven't found any enclosures, put semicolon at end of statement
            if (lastDelimiterPosition < 0)
            {
                lastDelimiterPosition = currentNode.Span.End;
            }

            // Move to space after the last delimiter
            args.TextView.TryMoveCaretToAndEnsureVisible(args.SubjectBuffer.CurrentSnapshot.GetPoint(GetEndPosition(root, lastDelimiterPosition, currentNode.Kind())));
            nextCommandHandler();
        }

        private bool IsCaretAtEndOfLine(SnapshotPoint? caret, int caretPosition)
        {
            return caret.Value.Position == caret.Value.GetContainingLine().End;
        }

        private SyntaxToken GetToken(SyntaxNode root, int caretPosition, SnapshotPoint? caret)
        {
            //previously bailed if caret was null, so this is safe
            if (IsCaretAtEndOfLine(caret, caretPosition))
                return root.FindToken(caretPosition - 1);
            else
                return root.FindToken(caretPosition);
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
                case SyntaxKind.ExpressionStatement:
                    return ((ExpressionStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.EmptyStatement:
                    return ((EmptyStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.GotoStatement:
                    return ((GotoStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.BreakStatement:
                    return ((BreakStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.ContinueStatement:
                    return ((ContinueStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.YieldReturnStatement:
                case SyntaxKind.YieldBreakStatement:
                    return ((YieldStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.LocalFunctionStatement:
                    return ((LocalFunctionStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.NamespaceDeclaration:
                    return ((NamespaceDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.UsingDirective:
                    return ((UsingDirectiveSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.ExternAliasDirective:
                    return ((ExternAliasDirectiveSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.EnumDeclaration:
                    return ((EnumDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.DelegateDeclaration:
                    return ((DelegateDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.EventFieldDeclaration:
                    return ((EventFieldDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.OperatorDeclaration:
                    return ((OperatorDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.ConversionOperatorDeclaration:
                    return ((ConversionOperatorDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                    return ((ConstructorDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.PropertyDeclaration:
                    return ((PropertyDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.AddAccessorDeclaration:
                    return ((AccessorDeclarationSyntax)currentNode).SemicolonToken.IsMissing;
                default:
                    return false;
            }
        }

        private static bool StatementClosingDelimiterExists(SyntaxNode currentNode, ref int lastDelimiterPosition)
        {
            switch (currentNode.Kind())
            {
                case SyntaxKind.DoStatement:
                    var dostatement = (DoStatementSyntax)currentNode;
                    if (dostatement.CloseParenToken.IsMissing)
                    {
                        return false;
                    }
                    else
                    {
                        lastDelimiterPosition = dostatement.CloseParenToken.Span.End;
                        return true;
                    }
                default:
                    // Statement I'm not handling yet so shouldn't proceed with statement completion
                    return false;
            }
        }

        private static bool ClosingDelimiterExistsIfNeeded(SyntaxNode currentNode, ref int lastDelimiterPosition)
        {
            switch (currentNode.Kind())
            {
                case SyntaxKind.ArgumentList:
                    var argumentList = (ArgumentListSyntax)currentNode;
                    if (argumentList.CloseParenToken.IsMissing)
                    {
                        return false;
                    }
                    else lastDelimiterPosition = argumentList.CloseParenToken.Span.End;
                    return true;
                case SyntaxKind.ParenthesizedExpression:
                    var parenthesizedExpression = (ParenthesizedExpressionSyntax)currentNode;
                    if (parenthesizedExpression.CloseParenToken.IsMissing)
                    {
                        return false;
                    }
                    else lastDelimiterPosition = parenthesizedExpression.CloseParenToken.Span.End;
                    return true;
                case SyntaxKind.BracketedArgumentList:
                    var bracketedArgumentList = (BracketedArgumentListSyntax)currentNode;
                    if (bracketedArgumentList.CloseBracketToken.IsMissing)
                    {
                        return false;
                    }
                    else lastDelimiterPosition = bracketedArgumentList.CloseBracketToken.Span.End;
                    return true;
                case SyntaxKind.ObjectInitializerExpression:
                    var initializerExpressionSyntax = (InitializerExpressionSyntax)currentNode;
                    if (initializerExpressionSyntax.CloseBraceToken.IsMissing)
                    {
                        return false;
                    }
                    else lastDelimiterPosition = initializerExpressionSyntax.CloseBraceToken.Span.End;
                    return true;
                default:
                    // Type of node does not require a closing delimiter
                    return true;
            }
        }

        private bool ReachedSemicolonSyntax(SyntaxNode currentNode, ISyntaxFactsService syntaxFacts, ref bool finalDelimiterNeedsSemicolon)
        {
            finalDelimiterNeedsSemicolon = false;

            if (currentNode.IsKind(SyntaxKind.DoStatement))
            {
                finalDelimiterNeedsSemicolon = true;
                return true;
            }

            if (syntaxFacts.IsStatement(currentNode)
                || currentNode.IsKind(SyntaxKind.GetAccessorDeclaration)
                || currentNode.IsKind(SyntaxKind.SetAccessorDeclaration)
                || currentNode.IsKind(SyntaxKind.LocalDeclarationStatement))
            {
                return true;
            }

            if (currentNode.IsKind(SyntaxKind.VariableDeclaration))
            {
                if (!currentNode.Ancestors().Any(n => n.IsKind(SyntaxKind.LocalDeclarationStatement)))
                {
                    return true;
                }
                if (currentNode.Ancestors().Any(n => n.IsKind(SyntaxKind.ForStatement)))
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// To account for the new line character at the end of a line, this returns the previous tokens end  
        /// </summary>
        private int GetEndPosition(SyntaxNode root, int end, SyntaxKind nodeKind)
        {

            // If "end" is at the end of a line, the token has trailing end of line trivia.
            // We want to put our cursor before that trivia, so use previous token for placement.
            var token = root.FindToken(end);
            if (token.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia))
            {
                return token.TrailingTrivia.Span.Start;
            }
            return end;
                //var previousToken = root.FindToken(end).GetPreviousToken();
                //return previousToken.Span.End;
        }

        private bool ApplicableToken(SyntaxToken token, SnapshotPoint? caret, int caretPosition)
        {
            if (caret == null)
            {
                return false;
            }

            if (caretIsBetweenFirstTokenAndOpeningBrace(token, caretPosition))
            {
                return false;
            }

            switch (token.Kind())
            {
                case SyntaxKind.OpenParenToken:
                    {
                        if (caretPosition == token.SpanStart)
                        {
                            return false;
                        }
                        var previousKind = token.GetPreviousToken().Kind();
                        if ( previousKind == SyntaxKind.IdentifierToken
                            || previousKind == SyntaxKind.EqualsExpression)
                        {
                            return false;
                        }
                        return true;
                    }
                case SyntaxKind.EqualsToken:
                    {
                        return false;
                    }
                case SyntaxKind.BreakKeyword:
                case SyntaxKind.ContinueKeyword:
                case SyntaxKind.EmptyStatement:
                    {
                        return false;
                    }
                case SyntaxKind.IdentifierToken:
                    {
                        if (caretPosition == token.Span.End && token.GetNextToken().Kind() != SyntaxKind.DotToken)
                        {
                            return true;
                        }
                        if (caretPosition == token.SpanStart && token.GetPreviousToken().Kind() != SyntaxKind.DotToken)
                        {
                            return true;
                        }
                        return false;
                    }
                case SyntaxKind.DotToken:
                    {
                        return false;
                    }
                case SyntaxKind.InterpolatedStringTextToken:
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.CharacterLiteralToken:
                    if (caretPosition >= token.Span.End || caretPosition <= token.SpanStart)
                    {
                        return true;
                    }
                    return false;
            }
            return true;
        }

        private bool caretIsBetweenFirstTokenAndOpeningBrace(SyntaxToken token, int caretPosition)
        {

            if (caretPosition == token.SpanStart)
            {
                if ((token.Kind() != SyntaxKind.CloseParenToken && token.GetPreviousToken().Kind() == SyntaxKind.OpenParenToken)
                    || (token.Kind() != SyntaxKind.CloseBraceToken && token.GetPreviousToken().Kind() == SyntaxKind.OpenBraceToken)
                    || (token.Kind() != SyntaxKind.CloseBracketToken && token.GetPreviousToken().Kind() == SyntaxKind.OpenBracketToken))
                {
                return true;
                }
            }

            return false;
        }

        public VSCommanding.CommandState GetCommandState(TypeCharCommandArgs args, Func<VSCommanding.CommandState> nextCommandHandler) => nextCommandHandler();

    }
}