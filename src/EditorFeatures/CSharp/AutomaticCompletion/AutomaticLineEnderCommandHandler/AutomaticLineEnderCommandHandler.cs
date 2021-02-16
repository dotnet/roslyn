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
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
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
    internal partial class AutomaticLineEnderCommandHandler : AbstractAutomaticLineEnderCommandHandler
    {
        private static readonly string s_semicolon = SyntaxFacts.GetText(SyntaxKind.SemicolonToken);

        /// <summary>
        /// Annotation to locate the open brace token.
        /// </summary>
        private static readonly SyntaxAnnotation s_openBracePositionAnnotation = new();

        /// <summary>
        /// Annotation to locate the replacement node(with or without braces).
        /// </summary>
        private static readonly SyntaxAnnotation s_replacementNodeAnnotation = new();

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
            bool addBrace,
            int caretPosition,
            CancellationToken cancellationToken)
        {
            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            // Add braces for the selected node
            if (addBrace)
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
                    var (newRoot, nextCaretPosition) = AddBraceToSelectedNode(document, root, selectedNode, args.TextView.Options, cancellationToken);
                    if (document.Project.Solution.Workspace.TryApplyChanges(document.WithSyntaxRoot(newRoot).Project.Solution))
                    {
                        args.TextView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(args.SubjectBuffer.CurrentSnapshot, nextCaretPosition));
                    }
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
                }
            }
            else
            {
                // Remove the braces and get the next caretPosition
                var (newRoot, nextCaretPosition) = RemoveBraceFromSelectedNode(
                    document,
                    root,
                    selectedNode,
                    args.TextView.Options,
                    cancellationToken);

                if (document.Project.Solution.Workspace.TryApplyChanges(document.WithSyntaxRoot(newRoot).Project.Solution))
                {
                    args.TextView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(args.SubjectBuffer.CurrentSnapshot, nextCaretPosition));
                }
            }
        }

        private static (SyntaxNode newRoot, int nextCaretPosition) AddBraceToSelectedNode(
            Document document,
            SyntaxNode root,
            SyntaxNode selectedNode,
            IEditorOptions editorOptions,
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
                    WithBraces(selectedNode, editorOptions),
                    cancellationToken);
                var nextCaretPosition = GetOpenBraceSpanEnd(newRoot);
                return (newRoot, nextCaretPosition);
            }

            if (selectedNode is ObjectCreationExpressionSyntax objectCreationExpressionNode)
            {
                // For ObjectCreationExpression, like new List<int>()
                // make sure it has '()' after the type, and if its next token is a missing semicolon, add that semicolon. e.g
                // var c = new Obje$$ct() => var c = new Object();
                var (newNode, oldNode) = ModifyObjectCreationExpressionNode(objectCreationExpressionNode, addOrRemoveInitializer: true, editorOptions);
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
                return AddBraceToEmbeddedStatementOwner(document, root, selectedNode, editorOptions, cancellationToken);
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static (SyntaxNode newRoot, int nextCaretPosition) RemoveBraceFromSelectedNode(
            Document document,
            SyntaxNode root,
            SyntaxNode selectedNode,
            IEditorOptions editorOptions,
            CancellationToken cancellationToken)
        {
            if (selectedNode is ObjectCreationExpressionSyntax objectCreationExpressionNode)
            {
                var (newNode, oldNode) = ModifyObjectCreationExpressionNode(objectCreationExpressionNode, addOrRemoveInitializer: false, editorOptions);
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
            bool addOrRemoveInitializer,
            IEditorOptions editorOptions)
        {
            var objectCreationNodeWithArgumentList = WithArgumentListIfNeeded(objectCreationExpressionNode);
            var objectCreationNodeWithInitializer = addOrRemoveInitializer
                ? WithBraces(objectCreationNodeWithArgumentList, editorOptions)
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
            IEditorOptions editorOptions,
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
                    WithBraces(selectedNode, editorOptions), cancellationToken);
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
                    AddBlockToEmbeddedStatementOwner(selectedNode, editorOptions),
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
                        AddBlockToEmbeddedStatementOwner(selectedNode, editorOptions),
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
                    AddBlockToEmbeddedStatementOwner(selectedNode, editorOptions, statement),
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
                        AddBlockToEmbeddedStatementOwner(selectedNode, editorOptions),
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
                    AddBlockToEmbeddedStatementOwner(selectedNode, editorOptions, statement),
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
                    return AddBraceToEmbeddedStatementOwner(document, root, elseClauseNode.Statement, editorOptions, cancellationToken);
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
                    WithBraces(selectedNode, editorOptions),
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

        /// <summary>
        /// Remove the brace for the input syntax node
        /// For ObjectCreationExpressionSyntax, it would remove the initializer
        /// For PropertyDeclarationSyntax, it would change it to a FieldDeclaration
        /// For EventDeclarationSyntax, it would change it to eventFieldDeclaration
        /// For Accessor, it would change it to the empty version ending with semicolon.
        /// e.g get {} => get;
        /// </summary>
        private static SyntaxNode WithoutBraces(SyntaxNode node)
            => node switch
            {
                ObjectCreationExpressionSyntax objectCreationExpressionNode => RemoveInitializerForObjectCreationExpression(objectCreationExpressionNode),
                PropertyDeclarationSyntax propertyDeclarationNode => ConvertPropertyDeclarationToFieldDeclaration(propertyDeclarationNode),
                EventDeclarationSyntax eventDeclarationNode => ConvertEventDeclarationToEventFieldDeclaration(eventDeclarationNode),
                AccessorDeclarationSyntax accessorDeclarationNode => RemoveBodyForAccessorDeclarationNode(accessorDeclarationNode),
                _ => throw ExceptionUtilities.UnexpectedValue(node),
            };

        /// <summary>
        /// Add braces to the <param name="node"/>.
        /// </summary>
        private static SyntaxNode WithBraces(SyntaxNode node, IEditorOptions editorOptions)
            => node switch
            {
                BaseTypeDeclarationSyntax baseTypeDeclarationNode => WithBracesForBaseTypeDeclaration(baseTypeDeclarationNode, editorOptions),
                ObjectCreationExpressionSyntax objectCreationExpressionNode => GetObjectCreationExpressionWithInitializer(objectCreationExpressionNode, editorOptions),
                FieldDeclarationSyntax fieldDeclarationNode when fieldDeclarationNode.Declaration.Variables.IsSingle()
                    => ConvertFieldDeclarationToPropertyDeclaration(fieldDeclarationNode, editorOptions),
                EventFieldDeclarationSyntax eventFieldDeclarationNode => ConvertEventFieldDeclarationToEventDeclaration(eventFieldDeclarationNode, editorOptions),
                BaseMethodDeclarationSyntax baseMethodDeclarationNode => AddBlockToBaseMethodDeclaration(baseMethodDeclarationNode, editorOptions),
                LocalFunctionStatementSyntax localFunctionStatementNode => AddBlockToLocalFunctionDeclaration(localFunctionStatementNode, editorOptions),
                AccessorDeclarationSyntax accessorDeclarationNode => AddBlockToAccessorDeclaration(accessorDeclarationNode, editorOptions),
                _ when node.IsEmbeddedStatementOwner() => AddBlockToEmbeddedStatementOwner(node, editorOptions),
                _ => throw ExceptionUtilities.UnexpectedValue(node),
            };

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
            var bracePair = GetBracePairString(textView.Options);

            // 1. Insert { \r\n }.
            var newDocument = document.InsertText(insertionPosition, bracePair, cancellationToken);

            // 2. Place caret between the braces.
            textView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(textView.TextSnapshot, insertionPosition + 1));

            // 3. Format the document using the close brace.
            FormatAndApplyBasedOnEndToken(newDocument, insertionPosition + bracePair.Length - 1, cancellationToken);
        }

        protected override (SyntaxNode selectedNode, bool addBrace)? GetValidNodeToModifyBraces(Document document, int caretPosition, CancellationToken cancellationToken)
        {
            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var token = root.FindTokenOnLeftOfPosition(caretPosition);
            if (token.IsKind(SyntaxKind.None))
            {
                return null;
            }

            foreach (var node in token.GetAncestors<SyntaxNode>())
            {
                if (ShouldAddBraces(node, caretPosition))
                {
                    return (node, true);
                }

                if (ShouldRemoveBraces(node, caretPosition))
                {
                    return (node, false);
                }
            }

            return null;
        }

        private static bool ShouldRemoveBraces(SyntaxNode node, int caretPosition)
            => node switch
            {
                ObjectCreationExpressionSyntax objectCreationExpressionNode => ShouldRemoveBraceForObjectCreationExpression(objectCreationExpressionNode),
                AccessorDeclarationSyntax accessorDeclarationNode => ShouldRemoveBraceForAccessorDeclaration(accessorDeclarationNode, caretPosition),
                PropertyDeclarationSyntax propertyDeclarationNode => ShouldRemoveBraceForPropertyDeclaration(propertyDeclarationNode, caretPosition),
                EventDeclarationSyntax eventDeclarationNode => ShouldRemoveBraceForEventDeclaration(eventDeclarationNode, caretPosition),
                _ => false,
            };

        private static bool ShouldAddBraces(SyntaxNode node, int caretPosition)
            => node switch
            {
                NamespaceDeclarationSyntax namespaceDeclarationNode => ShouldAddBraceForNamespaceDeclaration(namespaceDeclarationNode, caretPosition),
                BaseTypeDeclarationSyntax baseTypeDeclarationNode => ShouldAddBraceForBaseTypeDeclaration(baseTypeDeclarationNode, caretPosition),
                BaseMethodDeclarationSyntax baseMethodDeclarationNode => ShouldAddBraceForBaseMethodDeclaration(baseMethodDeclarationNode, caretPosition),
                LocalFunctionStatementSyntax localFunctionStatementNode => ShouldAddBraceForLocalFunctionStatement(localFunctionStatementNode, caretPosition),
                ObjectCreationExpressionSyntax objectCreationExpressionNode => ShouldAddBraceForObjectCreationExpression(objectCreationExpressionNode),
                BaseFieldDeclarationSyntax baseFieldDeclarationNode => ShouldAddBraceForBaseFieldDeclaration(baseFieldDeclarationNode),
                AccessorDeclarationSyntax accessorDeclarationNode => ShouldAddBraceForAccessorDeclaration(accessorDeclarationNode),
                IndexerDeclarationSyntax indexerDeclarationNode => ShouldAddBraceForIndexerDeclaration(indexerDeclarationNode, caretPosition),
                SwitchStatementSyntax switchStatementNode => ShouldAddBraceForSwitchStatement(switchStatementNode),
                TryStatementSyntax tryStatementNode => ShouldAddBraceForTryStatement(tryStatementNode, caretPosition),
                CatchClauseSyntax catchClauseNode => ShouldAddBraceForCatchClause(catchClauseNode, caretPosition),
                FinallyClauseSyntax finallyClauseNode => ShouldAddBraceForFinallyClause(finallyClauseNode, caretPosition),
                DoStatementSyntax doStatementNode => ShouldAddBraceForDoStatement(doStatementNode, caretPosition),
                CommonForEachStatementSyntax commonForEachStatementNode => ShouldAddBraceForCommonForEachStatement(commonForEachStatementNode, caretPosition),
                ForStatementSyntax forStatementNode => ShouldAddBraceForForStatement(forStatementNode, caretPosition),
                IfStatementSyntax ifStatementNode => ShouldAddBraceForIfStatement(ifStatementNode, caretPosition),
                ElseClauseSyntax elseClauseNode => ShouldAddBraceForElseClause(elseClauseNode, caretPosition),
                LockStatementSyntax lockStatementNode => ShouldAddBraceForLockStatement(lockStatementNode, caretPosition),
                UsingStatementSyntax usingStatementNode => ShouldAddBraceForUsingStatement(usingStatementNode, caretPosition),
                WhileStatementSyntax whileStatementNode => ShouldAddBraceForWhileStatement(whileStatementNode, caretPosition),
                _ => false,
            };

        private static string GetBracePairString(IEditorOptions editorOptions)
            => string.Concat(SyntaxFacts.GetText(SyntaxKind.OpenBraceToken),
                editorOptions.GetNewLineCharacter(),
                SyntaxFacts.GetText(SyntaxKind.CloseBraceToken));

        #endregion
    }
}
