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
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
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

        private void BeforeExecuteCommand(TypeCharCommandArgs args, CommandExecutionContext executionContext)
        {
            if (args.TypedChar != ';')
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

            // on the UI thread
            var root = document.GetSyntaxRootSynchronously(executionContext.OperationContext.UserCancellationToken);
            var caretPosition = caret.Position;

            var token = root.FindToken(caretPosition);

            var currentNode = token.Parent;

            // If the caret is right before an opening delimiter or right after a closing delimeter,
            // start analysis with node outside of delimiters.
            // Examples, 
            //    `obj.ToString$()` where `token` references `(` but the caret isn't actually inside the argument list.
            //    `obj.ToString()$` or `obj.method()$ .method()` where `token` references `)` but the caret isn't inside the argument list.
            if (token.IsKind(SyntaxKind.OpenBraceToken, SyntaxKind.OpenBracketToken, SyntaxKind.OpenParenToken) && token.Span.Start >= caretPosition
                || token.IsKind(SyntaxKind.CloseBraceToken, SyntaxKind.CloseBracketToken, SyntaxKind.CloseParenToken) && token.Span.End <= caretPosition)
            {
                currentNode = currentNode.Parent;
            }

            if (currentNode == null)
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (!LooksLikeNodeInArgumentList(currentNode, caret, syntaxFacts))
            {
                return;
            }

            // verify all delimiters exist until you reach statement syntax that requires a semicolon
            while (!IsStatementOrFieldDeclaration(currentNode, syntaxFacts))
            {
                if (RequiredDelimiterIsMissing(currentNode))
                {
                    // A required delimiter is missing; do not treat semicolon as statement completion
                    // Example: missing final `)` in `obj.Method($`
                    return;
                }

                if (currentNode.Parent == null)
                {
                    return;
                }

                currentNode = currentNode.Parent;
            }

            // if the statement syntax itself requires a closing delimiter, verify it is there
            if (StatementClosingDelimiterIsMissing(currentNode))
            {
                // Example: missing final `)` in `do { } while (x$$`
                return;
            }

            var semicolonPosition = GetSemicolonLocation(root, currentNode, caretPosition);

            // Place cursor after the statement
            args.TextView.TryMoveCaretToAndEnsureVisible(args.SubjectBuffer.CurrentSnapshot.GetPoint(semicolonPosition));
        }

        private static int GetSemicolonLocation(SyntaxNode root, SyntaxNode currentNode, int caretPosition)
        {
            int end;
            if (currentNode.IsKind(SyntaxKind.ForStatement))
            {
                // in for statements, semicolon can go after initializer or after condition, depending on where the caret is located
                var forStatementSyntax = (ForStatementSyntax)currentNode;
                if (CaretIsInForStatementCondition(caretPosition, forStatementSyntax))
                {
                    end = forStatementSyntax.Condition.FullSpan.End;
                }
                else if (CaretIsInForStatementDeclaration(caretPosition, forStatementSyntax))
                {
                    end = forStatementSyntax.Declaration.FullSpan.End;
                }
                else if (CaretIsInForStatementInitializers(caretPosition, forStatementSyntax))
                {
                    end = forStatementSyntax.Initializers.FullSpan.End;
                }
                else
                {
                    // Should not be reachable because we returned earlier for this case
                    throw ExceptionUtilities.Unreachable;
                }
            }
            else
            {
                end = currentNode.Span.End;
            }

            // If a node's semicolon is missing, the trailing trivia (including new line) is associated with the last token.
            // To avoid placing the semicolon on the next line or after comments, we need to adjust the position.
            return root.FindToken(end).GetPreviousToken().Span.End;
        }

        private static bool CaretIsInForStatementCondition(int caretPosition, ForStatementSyntax forStatementSyntax)
            // If condition is null and caret is in the condition section, as in `for ( ; $$; )`, 
            // we will have bailed earlier due to not being inside supported delimiters
            => forStatementSyntax.Condition == null
                ? false
                : caretPosition > forStatementSyntax.Condition.SpanStart &&
                  caretPosition < forStatementSyntax.Condition.Span.End;

        private static bool CaretIsInForStatementDeclaration(int caretPosition, ForStatementSyntax forStatementSyntax)
            => forStatementSyntax.Declaration != null &&
                caretPosition > forStatementSyntax.Declaration.Span.Start &&
                caretPosition < forStatementSyntax.Declaration.Span.End;

        private static bool CaretIsInForStatementInitializers(int caretPosition, ForStatementSyntax forStatementSyntax)
            => forStatementSyntax.Initializers.Count != 0 &&
                caretPosition > forStatementSyntax.Initializers.Span.Start &&
                caretPosition < forStatementSyntax.Initializers.Span.End;

        /// <summary>
        /// Examines the enclosing statement-like syntax for an expression which is eligible for statement completion.
        /// </summary>
        /// <remarks>
        /// <para>This method tries to identify <paramref name="currentNode"/> as a node located within an argument
        /// list, where the immediately-containing statement resembles an "expression statement". This method returns
        /// <see langword="true"/> if the node matches a recognizable pattern of this form.</para>
        /// </remarks>
        private static bool LooksLikeNodeInArgumentList(SyntaxNode currentNode,
            SnapshotPoint caret, ISyntaxFactsService syntaxFacts)
        {
            // work our way up the tree, looking for a node of interest within the current statement
            var nodeFound = false;
            while (!IsStatementOrFieldDeclaration(currentNode, syntaxFacts))
            {
                // This feature operates inside the following types of nodes, 
                // and can be expanded if more cases are implemented:
                // ArgumentList: covers method invocations like object.Method(arg)
                // ArrayRankSpecifier: covers new Type[dim]
                // ElementAccessExpression: covers indexer invocations like array[index]
                // ParenthesizedExpression: covers (3*(x+y))
                if (currentNode.IsKind(SyntaxKind.ArgumentList, SyntaxKind.ArrayRankSpecifier, SyntaxKind.BracketedArgumentList, SyntaxKind.ParenthesizedExpression))
                {
                    nodeFound = true;
                }

                // No special action is performed at this time if `;` is typed inside a string, including
                // interpolated strings.  
                if (IsInAString(currentNode, caret))
                {
                    return false;
                }

                // We reached the root without finding a statement
                if (currentNode.Parent == null)
                {
                    return false;
                }

                currentNode = currentNode.Parent;
            }

            // if we never found a statement, or a node of interest, or the statement kind is not a candidate for completion, return
            if (currentNode == null || !nodeFound || !StatementIsACandidate(currentNode, caret.Position))
            {
                return false;
            }

            return true;
        }

        private static bool IsStatementOrFieldDeclaration(SyntaxNode currentNode, ISyntaxFactsService syntaxFacts)
            => syntaxFacts.IsStatement(currentNode) || currentNode.IsKind(SyntaxKind.FieldDeclaration);

        private static bool IsInAString(SyntaxNode currentNode, SnapshotPoint caret)
            // Check to see if caret is before or after string
            => currentNode.IsKind(SyntaxKind.InterpolatedStringExpression, SyntaxKind.StringLiteralExpression)
                && caret.Position < currentNode.Span.End
                && caret.Position > currentNode.SpanStart;

        private static bool StatementIsACandidate(SyntaxNode currentNode, int caretPosition)
        {
            // if the statement kind ends in a semicolon, return true
            switch (currentNode.Kind())
            {
                case SyntaxKind.DoStatement:
                case SyntaxKind.ExpressionStatement:
                case SyntaxKind.GotoCaseStatement:
                case SyntaxKind.LocalDeclarationStatement:
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.ThrowStatement:
                case SyntaxKind.FieldDeclaration:
                    return true;
                case SyntaxKind.ForStatement:
                    var forStatementSyntax = (ForStatementSyntax)currentNode;
                    return CaretIsInForStatementCondition(caretPosition, forStatementSyntax) ||
                        CaretIsInForStatementDeclaration(caretPosition, forStatementSyntax) ||
                        CaretIsInForStatementInitializers(caretPosition, forStatementSyntax);
                default:
                    return false;
            }
        }

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
