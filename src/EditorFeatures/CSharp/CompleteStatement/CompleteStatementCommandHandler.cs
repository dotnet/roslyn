// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement
{
    /// <summary>
    /// When user types <c>;</c> in a statement, semicolon is added and caret is placed after the semicolon
    /// </summary>
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(CompleteStatementCommandHandler))]
    [Order(After = PredefinedCommandHandlerNames.Completion)]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal sealed class CompleteStatementCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
    {
        public VSCommanding.CommandState GetCommandState(TypeCharCommandArgs args, Func<VSCommanding.CommandState> nextCommandHandler) => nextCommandHandler();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompleteStatementCommandHandler()
        {
        }

        public string DisplayName => CSharpEditorResources.Complete_statement_on_semicolon;

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            // Determine where semicolon should be placed and move caret to location
            BeforeExecuteCommand(args, executionContext);

            // Insert the semicolon using next command handler
            nextCommandHandler();
        }

        private static void BeforeExecuteCommand(TypeCharCommandArgs args, CommandExecutionContext executionContext)
        {
            if (args.TypedChar != ';' || !args.TextView.Selection.IsEmpty)
            {
                return;
            }

            var caretOpt = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretOpt.HasValue)
            {
                return;
            }

            var caret = caretOpt.Value;
            var document = caret.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = document.GetSyntaxRootSynchronously(executionContext.OperationContext.UserCancellationToken);

            var cancellationToken = executionContext.OperationContext.UserCancellationToken;
            if (!TryGetStartingNode(root, caret, out var currentNode, cancellationToken))
            {
                return;
            }

            MoveCaretToSemicolonPosition(args, document, root, caret, syntaxFacts, currentNode,
                isInsideDelimiters: false, cancellationToken);
        }

        /// <summary>
        /// Determines which node the caret is in.  
        /// Must be called on the UI thread.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="caret"></param>
        /// <param name="startingNode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static bool TryGetStartingNode(SyntaxNode root, SnapshotPoint caret,
            out SyntaxNode startingNode, CancellationToken cancellationToken)
        {
            // on the UI thread
            startingNode = null;
            var caretPosition = caret.Position;

            var token = root.FindTokenOnLeftOfPosition(caretPosition);

            if (token.SyntaxTree == null
                || token.SyntaxTree.IsEntirelyWithinComment(caretPosition, cancellationToken))
            {
                return false;
            }

            startingNode = token.Parent;

            // If the caret is right before an opening delimiter or right after a closing delimeter,
            // start analysis with node outside of delimiters.
            // Examples, 
            //    `obj.ToString$()` where `token` references `(` but the caret isn't actually inside the argument list.
            //    `obj.ToString()$` or `obj.method()$ .method()` where `token` references `)` but the caret isn't inside the argument list.
            if (token.IsKind(SyntaxKind.OpenBraceToken, SyntaxKind.OpenBracketToken, SyntaxKind.OpenParenToken) && token.Span.Start >= caretPosition
                || token.IsKind(SyntaxKind.CloseBraceToken, SyntaxKind.CloseBracketToken, SyntaxKind.CloseParenToken) && token.Span.End <= caretPosition)
            {
                startingNode = startingNode.Parent;
            }

            return true;
        }

        private static void MoveCaretToSemicolonPosition(
            TypeCharCommandArgs args,
            Document document,
            SyntaxNode root,
            SnapshotPoint caret,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode currentNode,
            bool isInsideDelimiters,
            CancellationToken cancellationToken)
        {
            if (currentNode == null ||
                IsInAStringOrCharacter(currentNode, caret))
            {
                // Don't complete statement.  Return without moving the caret.
                return;
            }

            if (currentNode.IsKind(SyntaxKind.ArgumentList, SyntaxKind.ArrayRankSpecifier, SyntaxKind.BracketedArgumentList, SyntaxKind.ParenthesizedExpression, SyntaxKind.ParameterList))
            {
                // make sure the closing delimiter exists
                if (RequiredDelimiterIsMissing(currentNode))
                {
                    return;
                }

                // set caret to just outside the delimited span and analyze again
                // if caret was already in that position, return to avoid infinite loop
                var newCaretPosition = currentNode.Span.End;
                if (newCaretPosition == caret.Position)
                {
                    return;
                }

                var newCaret = args.SubjectBuffer.CurrentSnapshot.GetPoint(newCaretPosition);
                if (!TryGetStartingNode(root, newCaret, out currentNode, cancellationToken))
                {
                    return;
                }

                MoveCaretToSemicolonPosition(args, document, root, newCaret, syntaxFacts, currentNode,
                    isInsideDelimiters: true, cancellationToken);
            }
            else if (currentNode.IsKind(SyntaxKind.DoStatement))
            {
                if (IsInConditionOfDoStatement(currentNode, caret))
                {
                    MoveCaretToFinalPositionInStatement(currentNode, args, caret, true);
                }
                return;
            }
            else if (syntaxFacts.IsStatement(currentNode) || currentNode.IsKind(SyntaxKind.FieldDeclaration, SyntaxKind.DelegateDeclaration))
            {
                MoveCaretToFinalPositionInStatement(currentNode, args, caret, isInsideDelimiters);
                return;
            }
            else
            {
                // keep caret the same, but continue analyzing with the parent of the current node
                currentNode = currentNode.Parent;
                MoveCaretToSemicolonPosition(args, document, root, caret, syntaxFacts, currentNode,
                    isInsideDelimiters, cancellationToken);
                return;
            }
        }

        private static bool IsInConditionOfDoStatement(SyntaxNode currentNode, SnapshotPoint caret)
        {
            if (!currentNode.IsKind(SyntaxKind.DoStatement))
            {
                return false;
            }

            var condition = ((DoStatementSyntax)currentNode).Condition;
            return (caret >= condition.Span.Start && caret <= condition.Span.End);
        }

        private static void MoveCaretToFinalPositionInStatement(SyntaxNode statementNode, TypeCharCommandArgs args, SnapshotPoint caret, bool isInsideDelimiters)
        {
            if (StatementClosingDelimiterIsMissing(statementNode))
            {
                // Don't complete statement.  Return without moving the caret.
                return;
            }

            if (TryGetCaretPositionToMove(statementNode, caret, isInsideDelimiters, out var targetPosition))
            {
                Logger.Log(FunctionId.CommandHandler_CompleteStatement, KeyValueLogMessage.Create(LogType.UserAction, m =>
                {
                    m[nameof(isInsideDelimiters)] = isInsideDelimiters;
                    m[nameof(statementNode)] = statementNode.Kind();
                }));

                args.TextView.TryMoveCaretToAndEnsureVisible(targetPosition);
            }
        }

        private static bool TryGetCaretPositionToMove(SyntaxNode statementNode, SnapshotPoint caret, bool isInsideDelimiters, out SnapshotPoint targetPosition)
        {
            targetPosition = default;

            switch (statementNode.Kind())
            {
                case SyntaxKind.DoStatement:
                    //  Move caret after the do statment's closing paren.
                    targetPosition = caret.Snapshot.GetPoint(((DoStatementSyntax)statementNode).CloseParenToken.Span.End);
                    return true;
                case SyntaxKind.ForStatement:
                    // `For` statements can have semicolon after initializer/declaration or after condition.
                    // If caret is in initialer/declaration or condition, AND is inside other delimiters, complete statement
                    // Otherwise, return without moving the caret.
                    return isInsideDelimiters && TryGetForStatementCaret(caret, (ForStatementSyntax)statementNode, out targetPosition);
                case SyntaxKind.ExpressionStatement:
                case SyntaxKind.GotoCaseStatement:
                case SyntaxKind.LocalDeclarationStatement:
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.ThrowStatement:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.DelegateDeclaration:
                    // These statement types end in a semicolon. 
                    // if the original caret was inside any delimiters, `caret` will be after the outermost delimiter
                    targetPosition = caret;
                    return isInsideDelimiters;
                default:
                    // For all other statement types, don't complete statement.  Return without moving the caret.
                    return false;
            }
        }

        private static bool TryGetForStatementCaret(SnapshotPoint originalCaret, ForStatementSyntax forStatement, out SnapshotPoint forStatementCaret)
        {
            if (CaretIsInForStatementCondition(originalCaret, forStatement))
            {
                forStatementCaret = GetCaretAtPosition(forStatement.Condition.Span.End);
            }
            else if (CaretIsInForStatementDeclaration(originalCaret, forStatement))
            {
                forStatementCaret = GetCaretAtPosition(forStatement.Declaration.Span.End);
            }
            else if (CaretIsInForStatementInitializers(originalCaret, forStatement))
            {
                forStatementCaret = GetCaretAtPosition(forStatement.Initializers.Span.End);
            }
            else
            {
                // set caret to default, we will return false
                forStatementCaret = default;
            }

            return (forStatementCaret != default);

            // Locals
            SnapshotPoint GetCaretAtPosition(int position) => originalCaret.Snapshot.GetPoint(position);
        }

        private static bool CaretIsInForStatementCondition(int caretPosition, ForStatementSyntax forStatementSyntax)
            // If condition is null and caret is in the condition section, as in `for ( ; $$; )`, 
            // we will have bailed earlier due to not being inside supported delimiters
            => forStatementSyntax.Condition == null
                ? false
                : caretPosition > forStatementSyntax.Condition.SpanStart &&
                  caretPosition <= forStatementSyntax.Condition.Span.End;

        private static bool CaretIsInForStatementDeclaration(int caretPosition, ForStatementSyntax forStatementSyntax)
            => forStatementSyntax.Declaration != null &&
                caretPosition > forStatementSyntax.Declaration.Span.Start &&
                caretPosition <= forStatementSyntax.Declaration.Span.End;

        private static bool CaretIsInForStatementInitializers(int caretPosition, ForStatementSyntax forStatementSyntax)
            => forStatementSyntax.Initializers.Count != 0 &&
                caretPosition > forStatementSyntax.Initializers.Span.Start &&
                caretPosition <= forStatementSyntax.Initializers.Span.End;

        private static bool IsInAStringOrCharacter(SyntaxNode currentNode, SnapshotPoint caret)
            // Check to see if caret is before or after string
            => currentNode.IsKind(SyntaxKind.InterpolatedStringExpression, SyntaxKind.StringLiteralExpression, SyntaxKind.CharacterLiteralExpression)
                && caret.Position < currentNode.Span.End
                && caret.Position > currentNode.SpanStart;

        private static bool SemicolonIsMissing(SyntaxNode currentNode)
        {
            switch (currentNode.Kind())
            {
                case SyntaxKind.LocalDeclarationStatement:
                    return ((LocalDeclarationStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.ReturnStatement:
                    return ((ReturnStatementSyntax)currentNode).SemicolonToken.IsMissing;
                case SyntaxKind.VariableDeclaration:
                    return SemicolonIsMissing(currentNode.Parent);
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
                    // At this point, the node should be empty or its children should not end with a semicolon.
                    Debug.Assert(!currentNode.ChildNodesAndTokens().Any()
                        || !currentNode.ChildNodesAndTokens().Last().IsKind(SyntaxKind.SemicolonToken));
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
        /// <returns><see langword="true"/> if <paramref name="currentNode"/> is a statement that ends with a closing
        /// delimiter, and that closing delimiter exists in the source code; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool StatementClosingDelimiterIsMissing(SyntaxNode currentNode)
        {
            switch (currentNode.Kind())
            {
                case SyntaxKind.DoStatement:
                    var dostatement = (DoStatementSyntax)currentNode;
                    return dostatement.CloseParenToken.IsMissing;
                case SyntaxKind.ForStatement:
                    var forStatement = (ForStatementSyntax)currentNode;
                    return forStatement.CloseParenToken.IsMissing;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines if a syntax node includes all required closing delimiters.
        /// </summary>
        /// <remarks>
        /// <para>Some syntax nodes, such as parenthesized expressions, require a matching closing delimiter to end the
        /// syntax node. If this node is omitted from the source code, the parser will automatically insert a zero-width
        /// "missing" closing delimiter token to produce a valid syntax tree. This method determines if required closing
        /// delimiters are present in the original source.</para>
        /// </remarks>
        /// <param name="currentNode"></param>
        /// <returns>
        /// <list type="bullet">
        /// <item><description><see langword="true"/> if <paramref name="currentNode"/> requires a closing delimiter and the closing delimiter is present in the source (i.e. not missing)</description></item>
        /// <item><description><see langword="true"/> if <paramref name="currentNode"/> does not require a closing delimiter</description></item>
        /// <item><description>otherwise, <see langword="false"/>.</description></item>
        /// </list>
        /// </returns>
        private static bool RequiredDelimiterIsMissing(SyntaxNode currentNode)
        {
            switch (currentNode.Kind())
            {
                case SyntaxKind.ArgumentList:
                    var argumentList = (ArgumentListSyntax)currentNode;
                    return argumentList.CloseParenToken.IsMissing;

                case SyntaxKind.ParenthesizedExpression:
                    var parenthesizedExpression = (ParenthesizedExpressionSyntax)currentNode;
                    return parenthesizedExpression.CloseParenToken.IsMissing;

                case SyntaxKind.BracketedArgumentList:
                    var bracketedArgumentList = (BracketedArgumentListSyntax)currentNode;
                    return bracketedArgumentList.CloseBracketToken.IsMissing;

                case SyntaxKind.ObjectInitializerExpression:
                    var initializerExpressionSyntax = (InitializerExpressionSyntax)currentNode;
                    return initializerExpressionSyntax.CloseBraceToken.IsMissing;

                case SyntaxKind.ArrayRankSpecifier:
                    var arrayRankSpecifierSyntax = (ArrayRankSpecifierSyntax)currentNode;
                    return arrayRankSpecifierSyntax.CloseBracketToken.IsMissing;

                default:
                    // Type of node does not require a closing delimiter
                    return false;
            }
        }
    }
}
