// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
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
            BeforeExecuteCommand(args, executionContext);
            nextCommandHandler();
        }

        private void BeforeExecuteCommand(TypeCharCommandArgs args, CommandExecutionContext executionContext)
        {
            if (args.TypedChar != ';')
            {
                return;
            }

            var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caret.HasValue)
            {
                return;
            }

            var document = caret.Value.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var root = document.GetSyntaxRootSynchronously(executionContext.OperationContext.UserCancellationToken);
            var caretPosition = caret.Value.Position;

            var token = GetToken(root, caretPosition, caret.Value);

            var currentNode = token.Parent;

            // If cursor is right before an opening delimiter, make sure you start with node outside of delimiters. This
            // covers cases like `obj.ToString$()`, where `token` references `(` but the caret isn't actually inside the
            // argument list.
            if (token.IsKind(SyntaxKind.OpenBraceToken, SyntaxKind.OpenBracketToken, SyntaxKind.OpenParenToken)
                && token.Span.Start >= caretPosition)
            {
                currentNode = currentNode.Parent;
            }

            if (currentNode == null)
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (!LooksLikeNodeInArgumentListForStatementCompletion(currentNode, syntaxFacts))
            {
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
                    return;
                }

                if (currentNode.Parent == null)
                {
                    return;
                }

                currentNode = currentNode.Parent;
            }

            // if the statement syntax itself requires a closing delimeter, verify it is there
            if (finalDelimiterNeedsSemicolon)
            {
                if (!StatementClosingDelimiterExists(currentNode, ref lastDelimiterSpan))
                {
                    // Example: missing final `)` in `do { } while (x$$`
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
        }

        /// <summary>
        /// Examines the enclosing statement-like syntax for an expression which is eligible for statement completion.
        /// </summary>
        /// <remarks>
        /// <para>This method tries to identify <paramref name="currentNode"/> as a node located within an argument
        /// list, where the immediately-containing statement resembles an "expression statement". This method returns
        /// <see langword="true"/> if the node matches a recognizable pattern of this form.</para>
        /// </remarks>
        private static bool LooksLikeNodeInArgumentListForStatementCompletion(SyntaxNode currentNode, ISyntaxFactsService syntaxFacts)
        {
            while (!currentNode.IsKind(SyntaxKind.ArgumentList, SyntaxKind.ArrayRankSpecifier))
            {
                if (currentNode == null)
                {
                    // We reached the root without detecting a node of interest.
                    return false;
                }

                if (currentNode.IsKind(SyntaxKind.InterpolatedStringExpression, SyntaxKind.StringLiteralExpression))
                {
                    // No special action is performed at this time if `;` is typed inside a string, including
                    // interpolated strings.
                    return false;
                }

                if (syntaxFacts.IsStatement(currentNode)
                    || currentNode.IsKind(SyntaxKind.VariableDeclaration))
                {
                    // We reached an enclosing statement-like syntax without finding an intermediate argument-list-like
                    // syntax. Do not treat this statement as a candidate for statement completion because inserting a
                    // semicolon at the current location is likely to create a valid statement.
                    //
                    // IsStatement: The syntax is a known statement syntax
                    // VariableDelaration: The expression is part of a field initializer, which is not part of a
                    //      statement syntax but behaves like an expression statement for the purposes of statement
                    //      completion.
                    return false;
                }

                currentNode = currentNode.Parent;
            }

            // now we're in an argument list (or similar), so return the enclosing statement-like syntax
            while (!syntaxFacts.IsStatement(currentNode) && !currentNode.IsKind(SyntaxKind.VariableDeclaration))
            {
                currentNode = currentNode.Parent;
                if (currentNode == null)
                {
                    return false;
                }
            }

            Debug.Assert(currentNode != null);
            return true;
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

        /// <summary>
        /// Determines if a statement ends with a closing delimiter, and that closing delimiter exists.
        /// </summary>
        /// <remarks>
        /// <para>Statements such as <c>do { } while (expression);</c> contain embedded enclosing delimiters immediately
        /// preceding the semicolon. These delimiters are not part of the expression, but they behave like an argument
        /// list for the purposes of identifying relevant places for statement completion:</para>
        /// <list type="bullet">
        /// <item><description>The closing delimiter is typically inserted by the Automatic Brace Compeltion feature.</description></item>
        /// <item><description>It is not syntactically valid to place a semicolon <em>directly</em> within the delimiters.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="currentNode"></param>
        /// <param name="lastDelimiterSpan"></param>
        /// <returns><see langword="true"/> if <paramref name="currentNode"/> is a statement that ends with a closing
        /// delimiter, and that closing delimiter exists in the source code; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool StatementClosingDelimiterExists(SyntaxNode currentNode, ref TextSpan lastDelimiterSpan)
        {
            switch (currentNode.Kind())
            {
                case SyntaxKind.DoStatement:
                    var dostatement = (DoStatementSyntax)currentNode;
                    lastDelimiterSpan = dostatement.CloseParenToken.Span;
                    return !dostatement.CloseParenToken.IsMissing;

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
                    lastDelimiterSpan = argumentList.CloseParenToken.Span;
                    return !argumentList.CloseParenToken.IsMissing;

                case SyntaxKind.ParenthesizedExpression:
                    var parenthesizedExpression = (ParenthesizedExpressionSyntax)currentNode;
                    lastDelimiterSpan = parenthesizedExpression.CloseParenToken.Span;
                    return !parenthesizedExpression.CloseParenToken.IsMissing;

                case SyntaxKind.BracketedArgumentList:
                    var bracketedArgumentList = (BracketedArgumentListSyntax)currentNode;
                    lastDelimiterSpan = bracketedArgumentList.CloseBracketToken.Span;
                    return !bracketedArgumentList.CloseBracketToken.IsMissing;

                case SyntaxKind.ObjectInitializerExpression:
                    var initializerExpressionSyntax = (InitializerExpressionSyntax)currentNode;
                    lastDelimiterSpan = initializerExpressionSyntax.CloseBraceToken.Span;
                    return !initializerExpressionSyntax.CloseBraceToken.IsMissing;

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

                        if (token.GetPreviousToken().IsKind(SyntaxKind.IdentifierToken, SyntaxKind.EqualsExpression))
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
                        if (caretPosition == token.Span.End && !token.GetNextToken().IsKind(SyntaxKind.DotToken))
                        {
                            return true;
                        }

                        if (caretPosition == token.SpanStart && !token.GetPreviousToken().IsKind(SyntaxKind.DotToken))
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
                if ((!token.IsKind(SyntaxKind.CloseParenToken) && token.GetPreviousToken().IsKind(SyntaxKind.OpenParenToken))
                    || (!token.IsKind(SyntaxKind.CloseBraceToken) && token.GetPreviousToken().IsKind(SyntaxKind.OpenBraceToken))
                    || (!token.IsKind(SyntaxKind.CloseBracketToken) && token.GetPreviousToken().IsKind(SyntaxKind.OpenBracketToken)))
                {
                    return true;
                }
            }

            return false;
        }

        public VSCommanding.CommandState GetCommandState(TypeCharCommandArgs args, Func<VSCommanding.CommandState> nextCommandHandler) => nextCommandHandler();

    }
}
