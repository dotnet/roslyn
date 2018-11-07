// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
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

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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

            var token = GetToken(root, caretPosition, caret.Value);

            var currentNode = token.Parent;
            // if cursor is right before an opening delimiter, make sure you start with node outside of delimiters
            if (token.IsKind(SyntaxKind.OpenBraceToken, SyntaxKind.OpenBracketToken, SyntaxKind.OpenParenToken)
                && token.Span.Start >= caretPosition)
            {
                currentNode = currentNode.Parent;
            }

            if (currentNode == null)
            {
                nextCommandHandler();
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (GetEnclosingArgumentList(currentNode, syntaxFacts) == null)
            {
                nextCommandHandler();
                return;
            }

            var lastDelimiterSpan = default(TextSpan);
            var finalDelimiterNeedsSemicolon = false;

            // work your way out, verifying all delimeters exist until you reach statement syntax that requires a semicolon
            while (!ReachedSemicolonSyntax(currentNode, syntaxFacts, ref finalDelimiterNeedsSemicolon))
            {
                if (!ClosingDelimiterExistsIfNeeded(currentNode, ref lastDelimiterSpan))
                {
                    // A required delimiter is missing; do not treat semicolon as statement completion
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

            // if the statement syntax itself requires a closing delimeter, verify it is there
            if (finalDelimiterNeedsSemicolon)
            {
                if (!StatementClosingDelimiterExists(currentNode, ref lastDelimiterSpan))
                {
                    nextCommandHandler();
                    return;
                }
            }

            // if you haven't found any enclosures, put semicolon at end of statement
            if (lastDelimiterSpan == default)
            {
                lastDelimiterSpan = currentNode.Span;
            }

            // Move to space after the last delimiter
            args.TextView.TryMoveCaretToAndEnsureVisible(args.SubjectBuffer.CurrentSnapshot.GetPoint(GetEndPosition(root, lastDelimiterSpan.End, currentNode.Kind())));
            nextCommandHandler();
        }

        private static SyntaxNode GetEnclosingArgumentList(SyntaxNode currentNode, ISyntaxFactsService syntaxFacts)
        {
            while (currentNode.Kind() != SyntaxKind.ArgumentList
                && currentNode.Kind() != SyntaxKind.ArrayRankSpecifier)
            {
                if (currentNode.Kind() == SyntaxKind.InterpolatedStringExpression || currentNode.Kind() == SyntaxKind.StringLiteralExpression)
                {
                    return null;
                }

                if (currentNode == null
                    || syntaxFacts.IsStatement(currentNode)
                    || currentNode.Kind() == SyntaxKind.VariableDeclaration)
                {
                    return null;
                }

                currentNode = currentNode.Parent;
                if (currentNode == null)
                {
                    return null;
                }
            }

            // now we're in an argument list, so return the enclosing statement
            while (!syntaxFacts.IsStatement(currentNode) && !(currentNode.Kind() == SyntaxKind.VariableDeclaration))
            {
                currentNode = currentNode.Parent;
                if (currentNode == null)
                {
                    return null;
                }
            }

            return currentNode;
        }

        private static bool IsCaretAtEndOfLine(SnapshotPoint caret)
        {
            return caret.Position == caret.GetContainingLine().End;
        }

        private static SyntaxToken GetToken(SyntaxNode root, int caretPosition, SnapshotPoint caret)
        {
            if (IsCaretAtEndOfLine(caret) && caretPosition > 0)
            {
                return root.FindToken(caretPosition - 1);
            }
            else
            {
                return root.FindToken(caretPosition);
            }
        }

        private static bool SemiColonIsMissing(SyntaxNode currentNode)
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

        private static bool StatementClosingDelimiterExists(SyntaxNode currentNode, ref TextSpan lastDelimiterSpan)
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
                        lastDelimiterSpan = dostatement.CloseParenToken.Span;
                        return true;
                    }

                default:
                    // Statement I'm not handling yet so shouldn't proceed with statement completion
                    return false;
            }
        }

        private static bool ClosingDelimiterExistsIfNeeded(SyntaxNode currentNode, ref TextSpan lastDelimiterSpan)
        {
            switch (currentNode.Kind())
            {
                case SyntaxKind.ArgumentList:
                    var argumentList = (ArgumentListSyntax)currentNode;
                    if (argumentList.CloseParenToken.IsMissing)
                    {
                        return false;
                    }
                    else
                    {
                        lastDelimiterSpan = argumentList.CloseParenToken.Span;
                    }

                    return true;
                case SyntaxKind.ParenthesizedExpression:
                    var parenthesizedExpression = (ParenthesizedExpressionSyntax)currentNode;
                    if (parenthesizedExpression.CloseParenToken.IsMissing)
                    {
                        return false;
                    }
                    else
                    {
                        lastDelimiterSpan = parenthesizedExpression.CloseParenToken.Span;
                    }

                    return true;
                case SyntaxKind.BracketedArgumentList:
                    var bracketedArgumentList = (BracketedArgumentListSyntax)currentNode;
                    if (bracketedArgumentList.CloseBracketToken.IsMissing)
                    {
                        return false;
                    }
                    else
                    {
                        lastDelimiterSpan = bracketedArgumentList.CloseBracketToken.Span;
                    }

                    return true;
                case SyntaxKind.ObjectInitializerExpression:
                    var initializerExpressionSyntax = (InitializerExpressionSyntax)currentNode;
                    if (initializerExpressionSyntax.CloseBraceToken.IsMissing)
                    {
                        return false;
                    }
                    else
                    {
                        lastDelimiterSpan = initializerExpressionSyntax.CloseBraceToken.Span;
                    }

                    return true;
                default:
                    // Type of node does not require a closing delimiter
                    return true;
            }
        }

        private static bool ReachedSemicolonSyntax(SyntaxNode currentNode, ISyntaxFactsService syntaxFacts, ref bool finalDelimiterNeedsSemicolon)
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
                || currentNode.IsKind(SyntaxKind.LocalDeclarationStatement)
                || currentNode.IsKind(SyntaxKind.VariableDeclaration))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// To account for the new line character at the end of a line, this returns the previous tokens end  
        /// </summary>
        private static int GetEndPosition(SyntaxNode root, int end, SyntaxKind nodeKind)
        {

            // If "end" is at the end of a line, the token has trailing end of line trivia.
            // We want to put our cursor before that trivia, so use previous token for placement.
            var token = root.FindToken(end);

            if (token.SpanStart >= end)
            {
                // We found a token following the end position, which means 'end' is not the end of the line
                return end;
            }

            if (token.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia))
            {
                return token.TrailingTrivia.Span.Start;
            }

            return end;
            //var previousToken = root.FindToken(end).GetPreviousToken();
            //return previousToken.Span.End;
        }

        private static bool ApplicableToken(SyntaxToken token, SnapshotPoint? caret, int caretPosition)
        {
            if (caret == null)
            {
                return false;
            }

            if (CaretIsBetweenFirstTokenAndOpeningBrace(token, caretPosition))
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
                        if (previousKind == SyntaxKind.IdentifierToken
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

        private static bool CaretIsBetweenFirstTokenAndOpeningBrace(SyntaxToken token, int caretPosition)
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
