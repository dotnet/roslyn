// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editing;
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

        private static readonly SyntaxAnnotation s_openBracePositionAnnotation = new();
        private static readonly SyntaxAnnotation s_replacementNodeAnnotation = new();

        private static readonly SyntaxToken s_openBrace = SyntaxFactory.Token(
            SyntaxTriviaList.Empty, SyntaxKind.OpenBraceToken, SyntaxTriviaList.Empty.Add(SyntaxFactory.CarriageReturnLineFeed))
            .WithAdditionalAnnotations(s_openBracePositionAnnotation);

        private static readonly SyntaxToken s_closeBrace = SyntaxFactory.Token(
            SyntaxTriviaList.Empty, SyntaxKind.CloseBraceToken, SyntaxTriviaList.Empty);

        private static readonly BlockSyntax s_blockNode = SyntaxFactory.Block()
            .WithOpenBraceToken(s_openBrace).WithCloseBraceToken(s_closeBrace);

        private static readonly InitializerExpressionSyntax s_initializerNode =
            SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression)
                .WithOpenBraceToken(s_openBrace).WithCloseBraceToken(s_closeBrace);

        private static readonly AccessorListSyntax s_accessorListNode =
            SyntaxFactory.AccessorList().WithOpenBraceToken(s_openBrace).WithCloseBraceToken(s_closeBrace);

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
            var span = GetFormattedTextSpan(root, endToken);
            if (span == null)
            {
                return document;
            }

            var options = document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var changes = Formatter.GetFormattedTextChanges(
                root,
                new[] { CommonFormattingHelpers.GetFormattingSpan(root, span.Value) },
                document.Project.Solution.Workspace,
                options,
                rules: null, // use default
                cancellationToken: cancellationToken);

            return document.ApplyTextChanges(changes, cancellationToken);
        }

        private static TextSpan? GetFormattedTextSpan(SyntaxNode root, SyntaxToken endToken)
        {
            if (endToken.IsMissing)
            {
                return null;
            }

            var ranges = FormattingRangeHelper.FindAppropriateRange(endToken, useDefaultRange: false);
            if (ranges == null)
            {
                return null;
            }

            var startToken = ranges.Value.Item1;
            if (startToken.IsMissing || startToken.Kind() == SyntaxKind.None)
            {
                return null;
            }

            return CommonFormattingHelpers.GetFormattingSpan(root, TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End));
        }

        #region SemicolonAppending

        protected override string? GetEndingString(Document document, int position, CancellationToken cancellationToken)
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
                    return null;
                }

                if (!CheckLocation(text, position, owningNode, lastToken))
                {
                    // If we failed this check, we indeed got the intended owner node and
                    // inserting line ender here would introduce errors.
                    return null;
                }

                // so far so good. we only add semi-colon if it makes statement syntax error free
                var textToParse = owningNode.NormalizeWhitespace().ToFullString() + s_semicolon;

                // currently, Parsing a field is not supported. as a workaround, wrap the field in a type and parse
                var node = ParseNode(tree, owningNode, textToParse);

                // Insert line ender if we didn't introduce any diagnostics, if not try the next owning node.
                if (node != null && !node.ContainsDiagnostics)
                {
                    return s_semicolon;
                }
            }

            return null;
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

        protected override void ModifySelectedNode(
            AutomaticLineEnderCommandArgs args,
            Document document,
            SyntaxNode selectedNode,
            int caretPosition,
            CancellationToken cancellationToken)
        {
            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            // Add braces for the selected node
            if (ShouldAddBraces(selectedNode, caretPosition))
            {
                // For these syntax node, braces pair could be easily added by modify the syntax tree
                if (selectedNode is BaseTypeDeclarationSyntax
                    or BaseMethodDeclarationSyntax
                    or LocalFunctionStatementSyntax
                    or FieldDeclarationSyntax
                    or EventFieldDeclarationSyntax
                    or AccessorDeclarationSyntax
                    or ObjectCreationExpressionSyntax
                    or WhileStatementSyntax
                    or ForEachStatementSyntax
                    or ForStatementSyntax
                    or LockStatementSyntax
                    or UsingStatementSyntax
                    or DoStatementSyntax
                    or IfStatementSyntax
                    or ElseClauseSyntax)
                {
                    // Add the braces and get the next caretPosition
                    var (newRoot, nextCaretPosition) = AddBraceToSelectedNode(document, root, selectedNode, cancellationToken);
                    if (document.Project.Solution.Workspace.TryApplyChanges(document.WithSyntaxRoot(newRoot).Project.Solution))
                    {
                        args.TextView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(args.SubjectBuffer.CurrentSnapshot, nextCaretPosition));
                    }

                    return;
                }
                else
                {
                    // For the rest of the syntax node,
                    // like try statement
                    // class Bar
                    // {
                    //      void Main()
                    //      {
                    //          tr$$y
                    //      }
                    // }
                    // In this case, the last close brace of 'void Main()' would be thought as a part of the try statement,
                    // and the last close brace of 'Bar' would be thought as a part of Main()
                    // So for these case, just find the missing open brace position and directly insert '()' to the document
                    // 1. Find the position to insert braces.
                    var insertionPosition = GetBraceInsertionPosition(selectedNode);

                    // 2. Insert the braces and move caret
                    InsertBraceAndMoveCaret(args.TextView, document, insertionPosition, cancellationToken);
                    return;
                }
            }

            // Check if we need to remove braces for the node
            if (ShouldRemoveBraces(selectedNode, caretPosition))
            {
                // Remove the braces and get the next caretPosition
                var (newRoot, nextCaretPosition) = RemoveBraceFromSelectedNode(
                    document,
                    root,
                    selectedNode,
                    cancellationToken);

                if (document.Project.Solution.Workspace.TryApplyChanges(document.WithSyntaxRoot(newRoot).Project.Solution))
                {
                    args.TextView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(args.SubjectBuffer.CurrentSnapshot, nextCaretPosition));
                }

                return;
            }

            // Should not reach here since checks have been done before calling the method
            throw ExceptionUtilities.Unreachable;
        }

        private static (SyntaxNode newRoot, int nextCaretPosition) AddBraceToSelectedNode(
            Document document,
            SyntaxNode root,
            SyntaxNode selectedNode,
            CancellationToken cancellationToken)
        {
            if (selectedNode is BaseTypeDeclarationSyntax
                or BaseMethodDeclarationSyntax
                or LocalFunctionStatementSyntax
                or FieldDeclarationSyntax
                or EventFieldDeclarationSyntax
                or AccessorDeclarationSyntax)
            {
                var newRoot = ReplaceNodeAndFormat(
                    document,
                    root,
                    selectedNode,
                    WithBraces(selectedNode),
                    cancellationToken);
                var nextCaretPosition = GetOpenBraceSpanEnd(newRoot);
                return (newRoot, nextCaretPosition);
            }

            if (selectedNode is ObjectCreationExpressionSyntax objectCreationExpressionNode)
            {
                // For ObjectCreationExpression, like new List<int>()
                // make sure it has '()' after the type, and if its next token is a missing semicolon, add that semicolon. e.g
                // var c = new Obje$$ct() => var c = new Object();
                var (newNode, oldNode) = ModifyObjectCreationExpressionNode(objectCreationExpressionNode, addOrRemoveInitializer: true);
                var newRoot = ReplaceNodeAndFormat(
                    document,
                    root,
                    oldNode,
                    newNode,
                    cancellationToken);

                var nextCaretPosition = GetOpenBraceSpanEnd(newRoot);
                return (newRoot, nextCaretPosition);
            }

            if (selectedNode.IsEmbeddedStatementOwner())
            {
                return AddBraceToEmbeddedStatementOwner(document, root, selectedNode, cancellationToken);
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static (SyntaxNode newRoot, int nextCaretPosition) RemoveBraceFromSelectedNode(
            Document document,
            SyntaxNode root,
            SyntaxNode selectedNode,
            CancellationToken cancellationToken)
        {
            if (selectedNode is ObjectCreationExpressionSyntax objectCreationExpressionNode)
            {
                var (newNode, oldNode) = ModifyObjectCreationExpressionNode(objectCreationExpressionNode, addOrRemoveInitializer: false);
                var newRoot = ReplaceNodeAndFormat(
                    document,
                    root,
                    oldNode,
                    newNode,
                    cancellationToken);

                var replacementNode = newRoot.GetAnnotatedNodes(s_replacementNodeAnnotation).Single();
                if (replacementNode is ObjectCreationExpressionSyntax)
                {
                    var nextToken = replacementNode.GetLastToken().GetNextToken();
                    if (nextToken.IsKind(SyntaxKind.SemicolonToken)
                        && !nextToken.IsMissing)
                    {
                        return (newRoot, nextToken.Span.End);
                    }
                }

                var nextCaretPosition = newRoot.GetAnnotatedNodes(s_replacementNodeAnnotation).Single().GetLastToken().Span.End;
                return (newRoot, nextCaretPosition);
            }
            else
            {
                var newRoot = ReplaceNodeAndFormat(
                    document,
                    root,
                    selectedNode,
                    WithoutBraces(selectedNode),
                    cancellationToken);
                var nextCaretPosition = newRoot.GetAnnotatedNodes(s_replacementNodeAnnotation).Single().GetLastToken().Span.End;
                return (newRoot, nextCaretPosition);
            }
        }

        private static (SyntaxNode newNode, SyntaxNode oldNode) ModifyObjectCreationExpressionNode(
            ObjectCreationExpressionSyntax objectCreationExpressionNode,
            bool addOrRemoveInitializer)
        {
            var objectCreationNodeWithArgumentList = WithArgumentListIfNeeded(objectCreationExpressionNode);
            var objectCreationNodeWithInitializer = addOrRemoveInitializer
                ? WithBraces(objectCreationNodeWithArgumentList)
                : WithoutBraces(objectCreationNodeWithArgumentList);
            // If the next token is a missing semicolon, like
            // var l = new Ba$$r() { }
            // Also add the semicolon
            var nextToken = objectCreationExpressionNode.GetLastToken(includeZeroWidth: true).GetNextToken(includeZeroWidth: true);
            if (nextToken.IsKind(SyntaxKind.SemicolonToken)
                && nextToken.IsMissing
                && nextToken.Parent != null
                && nextToken.Parent.Contains(objectCreationExpressionNode))
            {
                var objectCreationNodeContainer = nextToken.Parent;
                // Replace the old object creation node and add the semicolon token.
                // Note: need to move the trailing trivia of the old node after semicolon token
                // e.g.
                // var l = new Bar() {} // I am some comments
                // =>
                // var l = new Bar() {}; // I am some comments
                var replacementContainerNode = objectCreationNodeContainer.ReplaceSyntax(
                    nodes: SpecializedCollections.SingletonCollection(objectCreationExpressionNode),
                    (_, _) => objectCreationNodeWithInitializer.WithoutTrailingTrivia(),
                    tokens: SpecializedCollections.SingletonCollection(nextToken),
                    computeReplacementToken: (_, _) =>
                        SyntaxFactory.Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(objectCreationNodeWithInitializer.GetTrailingTrivia()),
                    trivia: Enumerable.Empty<SyntaxTrivia>(),
                    computeReplacementTrivia: (_, syntaxTrivia) => syntaxTrivia);
                return (replacementContainerNode, objectCreationNodeContainer);
            }
            else
            {
                return (objectCreationNodeWithInitializer, objectCreationExpressionNode);
            }
        }

        private static (SyntaxNode newRoot, int nextCaretPosition) AddBraceToEmbeddedStatementOwner(
            Document document,
            SyntaxNode root,
            SyntaxNode selectedNode,
            CancellationToken cancellationToken)
        {
            // If there is no statement, just add a block to it.
            var statement = selectedNode.GetEmbeddedStatement();
            if (statement == null || statement.IsMissing)
            {
                var newRoot = ReplaceNodeAndFormat(
                    document,
                    root,
                    selectedNode,
                    WithBraces(selectedNode), cancellationToken);
                var nextCaretPosition = GetOpenBraceSpanEnd(newRoot);
                return (newRoot, nextCaretPosition);
            }

            // If there is an statement in the embeddedStatementOwner,
            // move the old statement next to the statementOwner,
            // and insert a empty block into the statementOwner,
            // e.g.
            // before:
            // whi$$le(true)
            // var i = 1;
            // for this case 'var i = 1;' is thought as the inner statement,
            //
            // after:
            // while(true)
            // {
            //      $$
            // }
            // var i = 1;
            if (selectedNode is WhileStatementSyntax
                or ForEachStatementSyntax
                or ForStatementSyntax
                or LockStatementSyntax
                or UsingStatementSyntax)
            {
                return ReplaceStatementOwnerAndInsertStatement(
                    document,
                    root,
                    selectedNode,
                    WithBracesForEmbeddedStatementOwner(selectedNode),
                    ImmutableArray<SyntaxNode>.Empty.Add(statement),
                    cancellationToken);
            }

            if (selectedNode is DoStatementSyntax)
            {
                // If this do statement doesn't end with the 'while' parts
                // e.g:
                // before:
                // d$$o
                // Print("hello");
                // after:
                // do
                // {
                //     $$
                // }
                // Print("hello");
                if (selectedNode is DoStatementSyntax
                {
                    WhileKeyword: { IsMissing: true },
                    SemicolonToken: { IsMissing: true },
                    OpenParenToken: { IsMissing: true },
                    CloseParenToken: { IsMissing: true }
                })
                {
                    return ReplaceStatementOwnerAndInsertStatement(
                        document,
                        root,
                        selectedNode,
                        WithBracesForEmbeddedStatementOwner(selectedNode),
                        ImmutableArray<SyntaxNode>.Empty.Add(statement),
                        cancellationToken);
                }

                // if the do statement has 'while' as an end
                // e.g:
                // before:
                // d$$o
                // Print("hello");
                // while (true);
                // after:
                // do
                // {
                //     $$
                //      Print("hello");
                // } while(true);
                var newRoot = ReplaceNodeAndFormat(
                    document,
                    root,
                    selectedNode,
                    WithBracesForEmbeddedStatementOwner(selectedNode, statement),
                    cancellationToken);
                var nextCaretPosition = GetOpenBraceSpanEnd(newRoot);
                return (newRoot, nextCaretPosition);
            }

            if (selectedNode is IfStatementSyntax ifStatementNode)
            {
                // If this is just an if without else
                // e.g.
                // before:
                // if$$ (true)
                //   Print("Hello");
                // after:
                // if (true)
                // {
                //      $$
                // }
                //   Print("Hello");
                if (ifStatementNode.Else == null)
                {
                    return ReplaceStatementOwnerAndInsertStatement(document,
                        root,
                        selectedNode,
                        WithBracesForEmbeddedStatementOwner(selectedNode),
                        ImmutableArray<SyntaxNode>.Empty.Add(statement),
                        cancellationToken);
                }

                // If this IfStatement has else statement after
                // e.g.
                // before:
                // if (true)
                //     print("Hello");
                // else {}
                // after:
                // if (true)
                // {
                //     $$
                //     print("Hello");
                // }
                // else {}
                var newRoot = ReplaceNodeAndFormat(
                    document,
                    root,
                    selectedNode,
                    WithBracesForEmbeddedStatementOwner(selectedNode, statement),
                    cancellationToken);
                var nextCaretPosition = GetOpenBraceSpanEnd(newRoot);
                return (newRoot, nextCaretPosition);
            }

            if (selectedNode is ElseClauseSyntax elseClauseNode)
            {
                // If this is an 'els$$e if(true)' statement,
                // then treat it as the selected node is the nested if statement
                if (elseClauseNode.Statement is IfStatementSyntax)
                {
                    return AddBraceToEmbeddedStatementOwner(document, root, elseClauseNode.Statement, cancellationToken);
                }

                // Otherwise, it is just an ending else clause
                // e.g. before:
                // if (true)
                // {
                // } els$$e
                // var i = 10;
                // after:
                // if (true)
                // {
                // } els$$e
                // {
                //      $$
                // }
                // var i = 10;
                return ReplaceStatementOwnerAndInsertStatement(document,
                    root,
                    selectedNode,
                    WithBraces(selectedNode),
                    ImmutableArray<SyntaxNode>.Empty.Add(statement),
                    cancellationToken);
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static int GetOpenBraceSpanEnd(SyntaxNode root)
        {
            var annotatedOpenBraceToken = root.GetAnnotatedTokens(s_openBracePositionAnnotation).Single();
            return annotatedOpenBraceToken.Span.End;
        }

        private static (SyntaxNode newRoot, int nextCaretPosition) ReplaceStatementOwnerAndInsertStatement(
            Document document,
            SyntaxNode root,
            SyntaxNode selectedNode,
            SyntaxNode newNode,
            ImmutableArray<SyntaxNode> nodesToInsert,
            CancellationToken cancellationToken)
        {
            var rootEditor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            rootEditor.InsertAfter(selectedNode, nodesToInsert);
            rootEditor.ReplaceNode(selectedNode, newNode.WithAdditionalAnnotations(s_replacementNodeAnnotation));
            var newRoot = rootEditor.GetChangedRoot();

            var newNodeAfterInsertion = newRoot.GetAnnotatedNodes(s_replacementNodeAnnotation).Single();
            var formattingNode = newNodeAfterInsertion.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? newNodeAfterInsertion;
            var formattedNewRoot = Formatter.Format(
                newRoot,
                formattingNode.Span,
                document.Project.Solution.Workspace,
                cancellationToken: cancellationToken);
            var nextCaretPosition = formattedNewRoot.GetAnnotatedTokens(s_openBracePositionAnnotation).Single().Span.End;
            return (formattedNewRoot, nextCaretPosition);
        }

        private static SyntaxNode ReplaceNodeAndFormat(
            Document document,
            SyntaxNode root,
            SyntaxNode oldNode,
            SyntaxNode newNode,
            CancellationToken cancellationToken)
        {
            var annotatedNewNode = newNode.WithAdditionalAnnotations(s_replacementNodeAnnotation);
            var newRoot = root.ReplaceNode(
                oldNode,
                annotatedNewNode);
            var newNodeAfterInsertion = newRoot.GetAnnotatedNodes(s_replacementNodeAnnotation).Single();

            var formattingNode = newNodeAfterInsertion.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? newNodeAfterInsertion;
            var options = document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var formattedNewRoot = Formatter.Format(
                newRoot,
                formattingNode.Span,
                document.Project.Solution.Workspace,
                options,
                cancellationToken: cancellationToken);
            return formattedNewRoot;
        }

        private static ObjectCreationExpressionSyntax WithArgumentListIfNeeded(ObjectCreationExpressionSyntax objectCreationExpressionNode)
        {
            var argumentList = objectCreationExpressionNode.ArgumentList;
            var hasArgumentList = argumentList != null && !argumentList.IsMissing;
            if (!hasArgumentList)
            {
                // Make sure the trailing trivia is passed to the argument list
                // like var l = new List\r\n =>
                // var l = new List()\r\r
                var typeNode = objectCreationExpressionNode.Type;
                var newArgumentList = SyntaxFactory.ArgumentList().WithTriviaFrom(typeNode);
                var newTypeNode = typeNode.WithoutTrivia();
                return objectCreationExpressionNode.WithType(newTypeNode).WithArgumentList(newArgumentList);
            }

            return objectCreationExpressionNode;
        }

        private static SyntaxNode WithoutBraces(SyntaxNode node)
            => node switch
            {
                ObjectCreationExpressionSyntax objectCreationExpressionNode => objectCreationExpressionNode.WithInitializer(null),
                PropertyDeclarationSyntax propertyDeclarationNode => SyntaxFactory.FieldDeclaration(
                    propertyDeclarationNode.AttributeLists,
                    propertyDeclarationNode.Modifiers,
                    SyntaxFactory.VariableDeclaration(
                        propertyDeclarationNode.Type,
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(propertyDeclarationNode.Identifier))),
                    SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                EventDeclarationSyntax eventDeclarationNode => SyntaxFactory.EventFieldDeclaration(
                    eventDeclarationNode.AttributeLists,
                    eventDeclarationNode.Modifiers,
                    SyntaxFactory.VariableDeclaration(
                        eventDeclarationNode.Type,
                 SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(eventDeclarationNode.Identifier)))),
                AccessorDeclarationSyntax accessorDeclarationNode => accessorDeclarationNode
                    .WithBody(null).WithoutTrailingTrivia().WithSemicolonToken(
                        SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.SemicolonToken, SyntaxTriviaList.Empty)),
                _ => node,
            };

        private static SyntaxNode WithBraces(SyntaxNode node)
            => node switch
            {
                BaseTypeDeclarationSyntax baseTypeDeclarationNode =>
                    baseTypeDeclarationNode.WithOpenBraceToken(s_openBrace)
                        .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken)),
                FieldDeclarationSyntax fieldDeclarationNode when fieldDeclarationNode.Declaration.Variables.IsSingle() =>
                    SyntaxFactory.PropertyDeclaration(
                        fieldDeclarationNode.AttributeLists,
                        fieldDeclarationNode.Modifiers,
                        fieldDeclarationNode.Declaration.Type,
                        explicitInterfaceSpecifier: null,
                        identifier: fieldDeclarationNode.Declaration.Variables[0].Identifier,
                        accessorList: s_accessorListNode,
                        expressionBody: null,
                        initializer: null,
                        semicolonToken: SyntaxFactory.Token(SyntaxKind.None)).WithTriviaFrom(node),
                ObjectCreationExpressionSyntax objectCreationExpressionNode => objectCreationExpressionNode.WithInitializer(s_initializerNode),
                EventFieldDeclarationSyntax eventFieldDeclarationNode when eventFieldDeclarationNode.Declaration.Variables.IsSingle() =>
                    SyntaxFactory.EventDeclaration(
                        eventFieldDeclarationNode.AttributeLists,
                        eventFieldDeclarationNode.Modifiers,
                        eventFieldDeclarationNode.EventKeyword,
                        eventFieldDeclarationNode.Declaration.Type,
                        explicitInterfaceSpecifier: null,
                        identifier: eventFieldDeclarationNode.Declaration.Variables[0].Identifier,
                        accessorList: s_accessorListNode,
                        semicolonToken: SyntaxFactory.Token(SyntaxKind.None)).WithTriviaFrom(node),
                BaseMethodDeclarationSyntax baseMethodDeclarationNode =>
                    baseMethodDeclarationNode.WithBody(s_blockNode).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None)),
                LocalFunctionStatementSyntax localFunctionStatementNode =>
                    localFunctionStatementNode.WithBody(s_blockNode).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None)),
                AccessorDeclarationSyntax accessorDeclarationNode =>
                    accessorDeclarationNode.WithBody(s_blockNode).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None)),
                _ when node.IsEmbeddedStatementOwner() => WithBracesForEmbeddedStatementOwner(node),
                _ => throw ExceptionUtilities.Unreachable,
            };

        private static SyntaxNode WithBracesForEmbeddedStatementOwner(
            SyntaxNode embeddedStatementOwner,
            SyntaxNode? extraNodeInsertedBetweenBraces = null)
        {
            var block = extraNodeInsertedBetweenBraces is StatementSyntax statementNode
                ? s_blockNode.WithStatements(new SyntaxList<StatementSyntax>(statementNode))
                : s_blockNode;

            return embeddedStatementOwner switch
            {
                DoStatementSyntax doStatementNode => doStatementNode.WithStatement(block),
                ForEachStatementSyntax forEachStatementNode => forEachStatementNode.WithStatement(block),
                ForStatementSyntax forStatementNode => forStatementNode.WithStatement(block),
                IfStatementSyntax ifStatementNode => ifStatementNode.WithStatement(block),
                ElseClauseSyntax elseClauseNode => elseClauseNode.WithStatement(block),
                WhileStatementSyntax whileStatementNode => whileStatementNode.WithStatement(block),
                UsingStatementSyntax usingStatementNode => usingStatementNode.WithStatement(block),
                LockStatementSyntax lockStatementNode => lockStatementNode.WithStatement(block),
                _ => throw ExceptionUtilities.Unreachable
            };
        }

        private static int GetBraceInsertionPosition(SyntaxNode node)
            => node switch
            {
                NamespaceDeclarationSyntax => node.GetBraces().openBrace.SpanStart,
                IndexerDeclarationSyntax indexerNode => indexerNode.ParameterList.Span.End,
                SwitchStatementSyntax switchStatementNode => switchStatementNode.CloseParenToken.Span.End,
                TryStatementSyntax tryStatementNode => tryStatementNode.TryKeyword.Span.End,
                CatchClauseSyntax catchClauseNode => catchClauseNode.Block.SpanStart,
                FinallyClauseSyntax finallyClauseNode => finallyClauseNode.Block.SpanStart,
                _ => throw ExceptionUtilities.Unreachable,
            };

        private void InsertBraceAndMoveCaret(
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
            FormatAndApplyBasedOnEndToken(newDocument, insertionPosition + s_bracePair.Length - 1, cancellationToken);
        }

        protected override SyntaxNode? GetValidNodeToModifyBraces(Document document, int caretPosition, CancellationToken cancellationToken)
        {
            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var token = root.FindTokenOnLeftOfPosition(caretPosition);
            if (token.IsKind(SyntaxKind.None))
            {
                return null;
            }

            return token.GetAncestor(node => ShouldAddBraces(node, caretPosition) || ShouldRemoveBraces(node, caretPosition));
        }

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

        private static bool WithinBraces(SyntaxNode? node, int caretPosition)
        {
            var (openBrace, closeBrace) = node.GetBraces();
            return TextSpan.FromBounds(openBrace.SpanStart, closeBrace.Span.End).Contains(caretPosition);
        }

        private static bool ShouldRemoveBraces(SyntaxNode node, int caretPosition)
        {
            // Remove the braces if the ObjectCreationExpression has an empty Initializer.
            if (node is ObjectCreationExpressionSyntax objectCreationNode
                && objectCreationNode.Initializer != null
                && objectCreationNode.Initializer.Expressions.IsEmpty())
            {
                return true;
            }

            // Only do this when it is an accessor in property
            // Since it is illegal to have something like
            // int this[int i] { get; set;}
            // event EventHandler Bar {add; remove;}
            if (node is AccessorDeclarationSyntax { Body: not null, ExpressionBody: null, Parent: not null } accessorDeclarationNode
                && node.Parent.IsParentKind(SyntaxKind.PropertyDeclaration)
                && accessorDeclarationNode.Body!.Span.Contains(caretPosition))
            {
                return true;
            }

            // If a property just has an empty accessorList, like
            // int i $${ }
            // then remove the braces and change it to a field
            // int i;
            if (node is PropertyDeclarationSyntax propertyDeclarationNode
                && propertyDeclarationNode.AccessorList != null
                && propertyDeclarationNode.ExpressionBody == null)
            {
                var accessorList = propertyDeclarationNode.AccessorList;
                return accessorList.Span.Contains(caretPosition) && accessorList.Accessors.IsEmpty();
            }

            // If an event declaration just has an empty accessorList,
            // like
            // event EventHandler e$$  { }
            // then change it to a event field declaration
            // event EventHandler e;
            if (node is EventDeclarationSyntax eventDeclarationNode
                && eventDeclarationNode.AccessorList != null)
            {
                var accessorList = eventDeclarationNode.AccessorList;
                return accessorList.Span.Contains(caretPosition) && accessorList.Accessors.IsEmpty();
            }

            return false;
        }

        private static bool ShouldAddBraces(SyntaxNode node, int caretPosition)
        {
            // For namespace, make sure it has name there is no braces
            if (node is NamespaceDeclarationSyntax { Name: IdentifierNameSyntax identifierName }
                && !identifierName.Identifier.IsMissing
                && HasNoBrace(node)
                && !WithinAttributeLists(node, caretPosition)
                && !WithinBraces(node, caretPosition))
            {
                return true;
            }

            // For class/struct/enum ..., make sure it has name and there is no braces.
            if (node is BaseTypeDeclarationSyntax { Identifier: { IsMissing: false } }
                && HasNoBrace(node)
                && HasNoBrace(node)
                && !WithinAttributeLists(node, caretPosition)
                && !WithinBraces(node, caretPosition))
            {
                return true;
            }

            // For method, make sure it has a ParameterList, because later braces would be inserted after the Parameterlist
            if (node is BaseMethodDeclarationSyntax { ExpressionBody: null, Body: null, ParameterList: { IsMissing: false }, SemicolonToken: { IsMissing: true } } baseMethodNode
                && !WithinAttributeLists(node, caretPosition)
                && !WithinMethodBody(node, caretPosition))
            {
                // Make sure we don't insert braces for method in Interface.
                return !baseMethodNode.IsParentKind(SyntaxKind.InterfaceDeclaration);
            }

            // For local Function, make sure it has a ParameterList, because later braces would be inserted after the Parameterlist
            if (node is LocalFunctionStatementSyntax { ExpressionBody: null, Body: null, ParameterList: { IsMissing: false } }
               && !WithinAttributeLists(node, caretPosition) && !WithinMethodBody(node, caretPosition))
            {
                return true;
            }

            if (node is ObjectCreationExpressionSyntax { Initializer: null })
            {
                return true;
            }

            // Add braces for field and event field if they only have one variable, semicolon is missing & don't have readonly keyword
            // Example:
            // public int Bar$$ =>
            // public int Bar
            // {
            //      $$
            // }
            // This would change field to property, and change event field to event declaration
            if (node is BaseFieldDeclarationSyntax baseFieldDeclaration
                && baseFieldDeclaration.Declaration.Variables.Count == 1
                && baseFieldDeclaration.Declaration.Variables[0].Initializer == null
                && !baseFieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)
                && baseFieldDeclaration.SemicolonToken.IsMissing)
            {
                return true;
            }

            if (node is AccessorDeclarationSyntax { Body: null, ExpressionBody: null, SemicolonToken: { IsMissing: true } })
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
            if (node is IndexerDeclarationSyntax indexerNode && ShouldAddBraceForIndexer(indexerNode, caretPosition))
            {
                return true;
            }

            if (node is SwitchStatementSyntax { OpenParenToken: { IsMissing: false }, CloseParenToken: { IsMissing: false }, OpenBraceToken: { IsMissing: true } } switchStatementNode
                && !WithinBraces(switchStatementNode, caretPosition))
            {
                return true;
            }

            if (node is TryStatementSyntax { TryKeyword: { IsMissing: false }, Block: { OpenBraceToken: { IsMissing: true } } } tryStatementNode
                && !tryStatementNode.Block.Span.Contains(caretPosition))
            {
                return true;
            }

            if (node is CatchClauseSyntax { CatchKeyword: { IsMissing: false }, Block: { OpenBraceToken: { IsMissing: true } } } catchClauseNode
                && !catchClauseNode.Block.Span.Contains(caretPosition))
            {
                return true;
            }

            if (node is FinallyClauseSyntax { FinallyKeyword: { IsMissing: false }, Block: { OpenBraceToken: { IsMissing: true } } } finallyClauseNode
                && !finallyClauseNode.Block.Span.Contains(caretPosition))
            {
                return true;
            }

            // For all the embeddedStatementOwners,
            // if the embeddedStatement is not block, insert the the braces if its statement is not block.
            if (node is DoStatementSyntax { DoKeyword: { IsMissing: false }, Statement: not BlockSyntax } doStatementNode
                && doStatementNode.DoKeyword.Span.Contains(caretPosition))
            {
                return true;
            }

            if (node is CommonForEachStatementSyntax { Statement: not BlockSyntax, OpenParenToken: { IsMissing: false }, CloseParenToken: { IsMissing: false } }
                && !WithinEmbeddedStatement(node, caretPosition))
            {
                return true;
            }

            if (node is ForStatementSyntax { Statement: not BlockSyntax, OpenParenToken: { IsMissing: false }, CloseParenToken: { IsMissing: false } }
                && !WithinEmbeddedStatement(node, caretPosition))
            {
                return true;
            }

            if (node is IfStatementSyntax { Statement: not BlockSyntax, OpenParenToken: { IsMissing: false }, CloseParenToken: { IsMissing: false } }
                && !WithinEmbeddedStatement(node, caretPosition))
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
                //         } else i$$f (b)
                // After: if (a)
                //        {
                //        } else if (b)
                //        {
                //            $$
                //        }
                if (elseClauseNode.Statement is IfStatementSyntax)
                {
                    return ShouldAddBraces(elseClauseNode.Statement, caretPosition);
                }
                else
                {
                    return elseClauseNode.Statement is not BlockSyntax && !WithinEmbeddedStatement(node, caretPosition);
                }
            }

            if (node is LockStatementSyntax { Statement: not BlockSyntax, OpenParenToken: { IsMissing: false }, CloseParenToken: { IsMissing: false } }
                && !WithinEmbeddedStatement(node, caretPosition))
            {
                return true;
            }

            if (node is UsingStatementSyntax { Statement: not BlockSyntax, OpenParenToken: { IsMissing: false }, CloseParenToken: { IsMissing: false } }
                && !WithinEmbeddedStatement(node, caretPosition))
            {
                return true;
            }

            if (node is WhileStatementSyntax { Statement: not BlockSyntax, OpenParenToken: { IsMissing: false }, CloseParenToken: { IsMissing: false } }
                && !WithinEmbeddedStatement(node, caretPosition))
            {
                return true;
            }

            return false;
        }

        private static bool ShouldAddBraceForIndexer(IndexerDeclarationSyntax indexerNode, int caretPosition)
        {
            if (WithinAttributeLists(indexerNode, caretPosition) ||
                WithinBraces(indexerNode.AccessorList, caretPosition))
            {
                return false;
            }

            // Make sure it has brackets
            var (openBracket, closeBracket) = indexerNode.ParameterList.GetBrackets();
            if (openBracket.IsMissing || closeBracket.IsMissing)
            {
                return false;
            }

            // If both accessorList and body is empty
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
