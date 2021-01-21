// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion
{
    /// <summary>
    /// csharp automatic line ender command handler
    /// </summary>
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.AutomaticLineEnder)]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal class AutomaticLineEnderCommandHandler : AbstractAutomaticLineEnderCommandHandler
    {
        private static readonly string s_bracePair = string.Concat(
            SyntaxFacts.GetText(SyntaxKind.OpenBraceToken),
            Environment.NewLine,
            SyntaxFacts.GetText(SyntaxKind.CloseBraceToken));

        private static readonly string s_semicolon = SyntaxFacts.GetText(SyntaxKind.SemicolonToken);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AutomaticLineEnderCommandHandler(
            ITextUndoHistoryRegistry undoRegistry,
            IEditorOperationsFactoryService editorOperations)
            : base(undoRegistry, editorOperations)
        {
        }

        protected override void NextAction(IEditorOperations editorOperation, Action nextAction)
            => editorOperation.InsertNewLine();

        protected override bool TreatAsReturn(Document document, int caretPosition, CancellationToken cancellationToken)
        {
            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);

            var endToken = root.FindToken(caretPosition);
            if (endToken.IsMissing)
            {
                return false;
            }

            var tokenToLeft = root.FindTokenOnLeftOfPosition(caretPosition);
            var startToken = endToken.GetPreviousToken();

            // case 1:
            //      Consider code like so: try {|}
            //      With auto brace completion on, user types `{` and `Return` in a hurry.
            //      During typing, it is possible that shift was still down and not released after typing `{`.
            //      So we've got an unintentional `shift + enter` and also we have nothing to complete this, 
            //      so we put in a newline,
            //      which generates code like so : try { } 
            //                                     |
            //      which is not useful as : try {
            //                                  |
            //                               }
            //      To support this, we treat `shift + enter` like `enter` here.
            var afterOpenBrace = startToken.Kind() == SyntaxKind.OpenBraceToken
                              && endToken.Kind() == SyntaxKind.CloseBraceToken
                              && tokenToLeft == startToken
                              && endToken.Parent.IsKind(SyntaxKind.Block)
                              && FormattingRangeHelper.AreTwoTokensOnSameLine(startToken, endToken);

            return afterOpenBrace;
        }

        protected override Document FormatAndApplyBasedOnEndToken(Document document, int position, CancellationToken cancellationToken)
        {
            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);

            var endToken = root.FindToken(position);
            if (endToken.IsMissing)
            {
                return document;
            }

            var ranges = FormattingRangeHelper.FindAppropriateRange(endToken, useDefaultRange: false);
            if (ranges == null)
            {
                return document;
            }

            var startToken = ranges.Value.Item1;
            if (startToken.IsMissing || startToken.Kind() == SyntaxKind.None)
            {
                return document;
            }

            var span = TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End);
            var options = document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            var changes = Formatter.GetFormattedTextChanges(
                root,
                new[] { CommonFormattingHelpers.GetFormattingSpan(root, span) },
                document.Project.Solution.Workspace,
                options,
                rules: null, // use default
                cancellationToken: cancellationToken);

            return document.ApplyTextChanges(changes, cancellationToken);
        }

        #region SemicolonAppending
        protected override string? GetEndingString(Document document, int position, CancellationToken cancellationToken)
            => ShouldAppendSemicolon(document, position, cancellationToken) ? s_semicolon : null;

        private static bool ShouldAppendSemicolon(Document document, int position, CancellationToken cancellationToken)
        {
            // prepare expansive information from document
            var tree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);
            var root = tree.GetRoot(cancellationToken);
            var text = tree.GetText(cancellationToken);

            // Go through the set of owning nodes in leaf to root chain.
            foreach (var owningNode in GetOwningNodes(root, position))
            {
                if (!TryGetLastToken(text, position, owningNode, out var lastToken))
                {
                    // If we can't get last token, there is nothing more to do, just skip
                    // the other owning nodes and return.
                    return false;
                }

                if (!CheckLocation(text, position, owningNode, lastToken))
                {
                    // If we failed this check, we indeed got the intended owner node and
                    // inserting line ender here would introduce errors.
                    return false;
                }

                // so far so good. we only add semi-colon if it makes statement syntax error free
                var textToParse = owningNode.NormalizeWhitespace().ToFullString() + s_semicolon;

                // currently, Parsing a field is not supported. as a workaround, wrap the field in a type and parse
                var node = ParseNode(tree, owningNode, textToParse);

                // Insert line ender if we didn't introduce any diagnostics, if not try the next owning node.
                if (node != null && !node.ContainsDiagnostics)
                {
                    return true;
                }
            }

            return false;
        }

        private static SyntaxNode? ParseNode(SyntaxTree tree, SyntaxNode owningNode, string textToParse)
            => owningNode switch
            {
                BaseFieldDeclarationSyntax => SyntaxFactory.ParseCompilationUnit(WrapInType(textToParse), options: (CSharpParseOptions)tree.Options),
                BaseMethodDeclarationSyntax => SyntaxFactory.ParseCompilationUnit(WrapInType(textToParse), options: (CSharpParseOptions)tree.Options),
                BasePropertyDeclarationSyntax => SyntaxFactory.ParseCompilationUnit(WrapInType(textToParse), options: (CSharpParseOptions)tree.Options),
                StatementSyntax => SyntaxFactory.ParseStatement(textToParse, options: (CSharpParseOptions)tree.Options),
                UsingDirectiveSyntax => SyntaxFactory.ParseCompilationUnit(textToParse, options: (CSharpParseOptions)tree.Options),
                _ => null,
            };

        /// <summary>
        /// wrap field in type
        /// </summary>
        private static string WrapInType(string textToParse)
            => "class C { " + textToParse + " }";

        /// <summary>
        /// make sure current location is okay to put semicolon
        /// </summary>
        private static bool CheckLocation(SourceText text, int position, SyntaxNode owningNode, SyntaxToken lastToken)
        {
            var line = text.Lines.GetLineFromPosition(position);

            // if caret is at the end of the line and containing statement is expression statement
            // don't do anything
            if (position == line.End && owningNode is ExpressionStatementSyntax)
            {
                return false;
            }

            var locatedAtTheEndOfLine = LocatedAtTheEndOfLine(line, lastToken);

            // make sure that there is no trailing text after last token on the line if it is not at the end of the line
            if (!locatedAtTheEndOfLine)
            {
                var endingString = text.ToString(TextSpan.FromBounds(lastToken.Span.End, line.End));
                if (!string.IsNullOrWhiteSpace(endingString))
                {
                    return false;
                }
            }

            // check whether using has contents
            if (owningNode is UsingDirectiveSyntax u && u.Name.IsMissing)
            {
                return false;
            }

            // make sure there is no open string literals
            var previousToken = lastToken.GetPreviousToken();
            if (previousToken.Kind() == SyntaxKind.StringLiteralToken && previousToken.ToString().Last() != '"')
            {
                return false;
            }

            if (previousToken.Kind() == SyntaxKind.CharacterLiteralToken && previousToken.ToString().Last() != '\'')
            {
                return false;
            }

            // now, check embedded statement case
            if (owningNode.IsEmbeddedStatementOwner())
            {
                var embeddedStatement = owningNode.GetEmbeddedStatement();
                if (embeddedStatement == null || embeddedStatement.Span.IsEmpty)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// get last token of the given using/field/statement/expression bodied member if one exists
        /// </summary>
        private static bool TryGetLastToken(SourceText text, int position, SyntaxNode owningNode, out SyntaxToken lastToken)
        {
            lastToken = owningNode.GetLastToken(includeZeroWidth: true);

            // last token must be on the same line as the caret
            var line = text.Lines.GetLineFromPosition(position);
            var locatedAtTheEndOfLine = LocatedAtTheEndOfLine(line, lastToken);
            if (!locatedAtTheEndOfLine && text.Lines.IndexOf(lastToken.Span.End) != line.LineNumber)
            {
                return false;
            }

            // if we already have last semicolon, we don't need to do anything
            if (!lastToken.IsMissing && lastToken.Kind() == SyntaxKind.SemicolonToken)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// check whether the line is located at the end of the line
        /// </summary>
        private static bool LocatedAtTheEndOfLine(TextLine line, SyntaxToken lastToken)
            => lastToken.IsMissing && lastToken.Span.End == line.EndIncludingLineBreak;

        /// <summary>
        /// find owning usings/field/statement/expression-bodied member of the given position
        /// </summary>
        private static IEnumerable<SyntaxNode> GetOwningNodes(SyntaxNode root, int position)
        {
            // make sure caret position is somewhere we can find a token
            var token = root.FindTokenFromEnd(position);
            if (token.Kind() == SyntaxKind.None)
            {
                return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
            }

            return token.GetAncestors<SyntaxNode>()
                        .Where(AllowedConstructs)
                        .Select(OwningNode)
                        .WhereNotNull();
        }

        private static bool AllowedConstructs(SyntaxNode n)
            => n is StatementSyntax
                or BaseFieldDeclarationSyntax
                or UsingDirectiveSyntax
                or ArrowExpressionClauseSyntax;

        private static SyntaxNode? OwningNode(SyntaxNode n)
            => n is ArrowExpressionClauseSyntax ? n.Parent : n;

        #endregion

        #region BraceModification
        protected override void ModifyBraces(
            AutomaticLineEnderCommandArgs args,
            Document document,
            SyntaxNode selectedNode,
            int caretPosition,
            CancellationToken cancellationToken)
        {
            var newDocument = document;

            // Check if we need to add braces for the node.
            if (ShouldAddBraces(selectedNode))
            {
                // 1. Find the position to insert braces.
                var insertionPosition = GetBraceInsertionPosition(selectedNode);

                // 2. Insert the braces and move caret
                newDocument = InsertBraceAndMoveCaret(args.TextView, document, insertionPosition, cancellationToken);
            }

            // Check if we need to remove braces for the node
            // Currently only support ObjectCreationExpression with empty initializer.
            if (ShouldRemoveBraces(selectedNode))
            {
                // Remove the braces and move caret.
                newDocument = RemoveBraceAndMoveCaret(args.TextView, document, caretPosition, selectedNode, cancellationToken);
            }

            // Check if semicolon needs to be appended to the selectedNode
            // It is only needed by ObjectCreationExpressionSyntax.
            // Example:
            // Before: var a = new Bar()$$
            // After: var a = new Bar()
            // {
            //      $$
            // };
            var newCaret = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (selectedNode is ObjectCreationExpressionSyntax && newCaret.HasValue)
            {
                // Braces are add/removed from the syntax node, and the caret now should be placed between the braces,
                // so get the support node again.
                var supportedNode = GetSupportedNode(newDocument, newCaret.Value, cancellationToken);
                if (supportedNode is ObjectCreationExpressionSyntax)
                {
                    // Ensure we would only append the semicolon to the ObjectCreationExpression node
                    var shouldAppendSemicolon = ShouldAppendSemicolon(newDocument, supportedNode.GetLastToken().SpanStart, cancellationToken);
                    if (shouldAppendSemicolon)
                    {
                        newDocument.InsertText(supportedNode.GetLastToken().Span.End, s_semicolon, cancellationToken);
                    }
                }
            }
        }

        private static int GetBraceInsertionPosition(SyntaxNode node)
            => node switch
            {
                NamespaceDeclarationSyntax or BaseTypeDeclarationSyntax => node.GetBraces().openBrace.SpanStart,
                BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax => node.GetParameterList()!.Span.End,
                IndexerDeclarationSyntax indexerNode => indexerNode.ParameterList.Span.End,
                ObjectCreationExpressionSyntax objectCreationExpressionNode => objectCreationExpressionNode.GetLastToken().Span.End,
                DoStatementSyntax doStatementNode => doStatementNode.DoKeyword.Span.End,
                ForEachStatementSyntax forEachStatementNode => forEachStatementNode.CloseParenToken.Span.End,
                ForStatementSyntax forStatementNode => forStatementNode.CloseParenToken.Span.End,
                IfStatementSyntax ifStatementNode => ifStatementNode.CloseParenToken.Span.End,
                // In case it is an else-if clause, if the statement is IfStatement, use its insertion statement
                // otherwise, use the end of the else keyword
                // Example:
                // Before: if (a)
                //         {
                //         } el$$se if (b)
                // After: if (a)
                //        {
                //        } else if (b)
                //        {
                //            $$
                //        }
                ElseClauseSyntax elseClauseNode => elseClauseNode.Statement is IfStatementSyntax
                    ? GetBraceInsertionPosition(elseClauseNode.Statement)
                    : elseClauseNode.ElseKeyword.Span.End,
                LockStatementSyntax lockStatementNode => lockStatementNode.CloseParenToken.Span.End,
                UsingStatementSyntax usingStatementNode => usingStatementNode.CloseParenToken.Span.End,
                WhileStatementSyntax whileStatementNode => whileStatementNode.CloseParenToken.Span.End,
                SwitchStatementSyntax switchStatementNode => switchStatementNode.CloseParenToken.Span.End,
                TryStatementSyntax tryStatementNode => tryStatementNode.TryKeyword.Span.End,
                CatchClauseSyntax catchClauseNode => catchClauseNode.Block.SpanStart,
                _ => throw ExceptionUtilities.Unreachable,
            };

        private Document InsertBraceAndMoveCaret(
            ITextView textView,
            Document document,
            int insertionPosition,
            CancellationToken cancellationToken)
        {
            // 1. Insert { \r\n }.
            var newDocument = document.InsertText(insertionPosition, s_bracePair, cancellationToken);

            // 2. Place caret between the braces.
            textView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(textView.TextSnapshot, insertionPosition + 1));

            // 3. Format the document using the close brace.
            return FormatAndApplyBasedOnEndToken(newDocument, insertionPosition + s_bracePair.Length - 1, cancellationToken);
        }

        private Document RemoveBraceAndMoveCaret(
            ITextView textView,
            Document document,
            int caretPosition,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            // Only support remove braces for ObjectCreationExpression with Empty Initializer
            if (node is ObjectCreationExpressionSyntax objectCreationNode && objectCreationNode.Initializer != null)
            {
                // Move the caret the line end.
                var lineEnd = document.GetTextSynchronously(cancellationToken).Lines.GetLineFromPosition(caretPosition).End;
                textView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(textView.TextSnapshot, lineEnd));

                // Remove the initializer
                var initializer = objectCreationNode.Initializer;
                var newDocument = document.RemoveText(initializer.Span, cancellationToken);

                // Format the document based on the last token.
                return FormatAndApplyBasedOnEndToken(
                    newDocument,
                    objectCreationNode.GetLastToken().Span.End - initializer.Span.Length,
                    cancellationToken);
            }

            return document;
        }

        protected override SyntaxNode? GetValidNodeToModifyBraces(Document document, int caretPosition, CancellationToken cancellationToken)
        {
            // 1. Get the node supports add/remove braces.
            var supportedNode = GetSupportedNode(document, caretPosition, cancellationToken);

            // 2. Make sure the caret is placed at the 'header' of the node.
            // Example:
            // if (a)
            // Pri$$nt("")
            // Parser would think 'Print("")' is a part of the if statement, but we need to make sure
            // the caret is placed between 'if (a)'
            return IsCaretOnHeader(supportedNode, caretPosition) ? supportedNode : null;
        }

        private static SyntaxNode? GetSupportedNode(Document document, int caretPosition, CancellationToken cancellationToken)
        {
            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var token = root.FindTokenOnLeftOfPosition(caretPosition);
            if (token.IsKind(SyntaxKind.None))
            {
                return null;
            }

            return token.GetAncestor(node => ShouldAddBraces(node) || ShouldRemoveBraces(node));
        }

        private static bool IsCaretOnHeader(SyntaxNode? node, int caretPosition)
            => node switch
            {
                NamespaceDeclarationSyntax or BaseTypeDeclarationSyntax
                    => !WithinAttributeLists(node, caretPosition) && !WithinBraces(node, caretPosition),
                BaseMethodDeclarationSyntax {ExpressionBody: null} or LocalFunctionStatementSyntax {ExpressionBody: null}
                    => !WithinAttributeLists(node, caretPosition) && !WithinMethodBody(node, caretPosition),
                IndexerDeclarationSyntax {AccessorList: not null} indexerDeclarationNode
                    => !WithinAttributeLists(node, caretPosition) && !WithinBraces(indexerDeclarationNode.AccessorList, caretPosition),
                ObjectCreationExpressionSyntax objectCreationNode => objectCreationNode.FullSpan.Contains(caretPosition),
                DoStatementSyntax doStatementNode => doStatementNode.DoKeyword.FullSpan.Contains(caretPosition),
                ForEachStatementSyntax or ForStatementSyntax or IfStatementSyntax or LockStatementSyntax or UsingStatementSyntax or WhileStatementSyntax
                    => !WithinEmbeddedStatement(node, caretPosition),
                ElseClauseSyntax elseClauseNode => elseClauseNode.Statement is IfStatementSyntax
                    ? IsCaretOnHeader(elseClauseNode.Statement, caretPosition)
                    : !WithinEmbeddedStatement(node, caretPosition),
                SwitchStatementSyntax switchStatementNode => !WithinBraces(switchStatementNode, caretPosition),
                TryStatementSyntax tryStatementNode => !tryStatementNode.Block.Span.Contains(caretPosition),
                CatchClauseSyntax catchClauseNode => !catchClauseNode.Block.Span.Contains(caretPosition),
                _ => false,
            };

        private static bool WithinMethodBody(SyntaxNode node, int caretPosition)
        {
            if (node is BaseMethodDeclarationSyntax methodDeclarationNode)
            {
                return methodDeclarationNode.Body?.Span.Contains(caretPosition) ?? false;
            }

            if (node is LocalFunctionStatementSyntax localFunctionStatementNode)
            {
                return localFunctionStatementNode.Body?.Span.Contains(caretPosition) ?? false;
            }

            return false;
        }

        private static bool WithinEmbeddedStatement(SyntaxNode node, int caretPosition)
            => node.GetEmbeddedStatement()?.Span.Contains(caretPosition) ?? false;

        private static bool WithinAttributeLists(SyntaxNode node, int caretPosition)
        {
            var attributeLists = node.GetAttributeLists();
            return attributeLists.Span.Contains(caretPosition);
        }

        private static bool WithinBraces(SyntaxNode node, int caretPosition)
        {
            var (openBrace, closeBrace) = node.GetBraces();
            return TextSpan.FromBounds(openBrace.SpanStart, closeBrace.Span.End).Contains(caretPosition);
        }

        private static bool ShouldRemoveBraces(SyntaxNode node)
        {
            // Remove the braces if the ObjectCreationExpression has an empty Initializer.
            if (node is ObjectCreationExpressionSyntax objectCreationNode
                && objectCreationNode.Initializer != null)
            {
                var expressions = objectCreationNode.Initializer.Expressions;
                if (expressions.IsEmpty())
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldAddBraces(SyntaxNode node)
        {
            // For namespace, make sure it has name there is no braces
            if (node is NamespaceDeclarationSyntax {Name: IdentifierNameSyntax identifierName}
                && !identifierName.Identifier.IsMissing
                && HasNoBrace(node))
            {
                return true;
            }

            // For class/struct/enum ..., make sure it has name and there is no braces.
            if (node is BaseTypeDeclarationSyntax {Identifier: {IsMissing: false}} && HasNoBrace(node))
            {
                return true;
            }

            // For method, make sure it has a ParameterList, because later braces would be inserted after the Parameterlist
            if (node is BaseMethodDeclarationSyntax
                {ExpressionBody: null, Body: null, ParameterList: {IsMissing: false}, SemicolonToken: {IsMissing: true}} baseMethodNode)
            {
                // Make sure we don't insert braces for method in Interface.
                return !baseMethodNode.IsParentKind(SyntaxKind.InterfaceDeclaration);
            }

            // For local Function, make sure it has a ParameterList, because later braces would be inserted after the Parameterlist
            if (node is LocalFunctionStatementSyntax {ExpressionBody: null, Body: null, ParameterList: {IsMissing: false}})
            {
                return true;
            }

            if (node is ObjectCreationExpressionSyntax {Initializer: null})
            {
                return true;
            }

            // For indexer, switch, try and catch syntax node without braces, if it is the last child of its parent, it would
            // use its parent's close brace as its own.
            // Example:
            // class Bar
            // {
            //      int th$$is[int i]
            // }
            // In this case, parser would think the last '}' belongs to the indexer, not the class.
            // Therefore, only check if the open brace is missing for these 4 types of SyntaxNode
            if (node is IndexerDeclarationSyntax indexerNode && ShouldAddBraceForIndexer(indexerNode))
            {
                return true;
            }

            if (node is SwitchStatementSyntax {OpenParenToken: {IsMissing: false}, CloseParenToken: {IsMissing: false}, OpenBraceToken: { IsMissing: true}})
            {
                return true;
            }

            if (node is TryStatementSyntax {TryKeyword: {IsMissing: false}, Block: {OpenBraceToken: {IsMissing: true}}})
            {
                return true;
            }

            if (node is CatchClauseSyntax {CatchKeyword: {IsMissing: false}, Block: {OpenBraceToken: {IsMissing: true }}})
            {
                return true;
            }

            // For all the embeddedStatementOwners,
            // if the embeddedStatement is not block, insert the the braces if its statement is not block.
            if (node is DoStatementSyntax {DoKeyword: {IsMissing: false}, Statement: not BlockSyntax})
            {
                return true;
            }

            if (node is CommonForEachStatementSyntax {Statement: not BlockSyntax, OpenParenToken: {IsMissing: false}, CloseParenToken: {IsMissing: false}})
            {
                return true;
            }

            if (node is ForStatementSyntax {Statement: not BlockSyntax, OpenParenToken: {IsMissing: false}, CloseParenToken: {IsMissing: false}})
            {
                return true;
            }

            if (node is IfStatementSyntax {Statement: not BlockSyntax, OpenParenToken: {IsMissing: false}, CloseParenToken: {IsMissing: false}})
            {
                return true;
            }

            if (node is ElseClauseSyntax elseClauseNode)
            {
                // In case it is an else-if clause, if the statement is IfStatement, use its insertion statement
                // otherwise, use the end of the else keyword
                // Example:
                // Before: if (a)
                //         {
                //         } el$$se if (b)
                // After: if (a)
                //        {
                //        } else if (b)
                //        {
                //            $$
                //        }
                if (elseClauseNode.Statement is IfStatementSyntax)
                {
                    return ShouldAddBraces(elseClauseNode.Statement);
                }
                else
                {
                    return elseClauseNode.Statement is not BlockSyntax;
                }
            }

            if (node is LockStatementSyntax {Statement: not BlockSyntax, OpenParenToken: {IsMissing: false}, CloseParenToken: {IsMissing: false}})
            {
                return true;
            }

            if (node is UsingStatementSyntax {Statement: not BlockSyntax, OpenParenToken: {IsMissing: false}, CloseParenToken: {IsMissing: false}})
            {
                return true;
            }

            if (node is WhileStatementSyntax {Statement: not BlockSyntax, OpenParenToken: {IsMissing: false}, CloseParenToken: {IsMissing: false}})
            {
                return true;
            }

            return false;
        }

        private static bool ShouldAddBraceForIndexer(IndexerDeclarationSyntax indexerNode)
        {
            var (openBracket, closeBracket) = indexerNode.ParameterList.GetBrackets();
            if (openBracket.IsMissing || closeBracket.IsMissing)
            {
                return false;
            }

            // 1. If there is no AccessorList and Expression Body.
            if ((indexerNode.AccessorList == null || indexerNode.AccessorList.IsMissing)
                && indexerNode.ExpressionBody == null)
            {
                return true;
            }

            return indexerNode.AccessorList != null
               && indexerNode.AccessorList.OpenBraceToken.IsMissing;
        }

        private static bool HasNoBrace(SyntaxNode node)
        {
            var (openBrace, closeBrace) = node.GetBraces();
            return openBrace.IsKind(SyntaxKind.None) && closeBrace.IsKind(SyntaxKind.None)
                || openBrace.IsMissing && closeBrace.IsMissing;
        }
        #endregion
    }
}
