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
using Microsoft.CodeAnalysis.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
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
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion
{
    /// <summary>
    /// csharp automatic line ender command handler
    /// </summary>
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.AutomaticLineEnder)]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal partial class AutomaticLineEnderCommandHandler(
        ITextUndoHistoryRegistry undoRegistry,
        IEditorOperationsFactoryService editorOperations,
        EditorOptionsService editorOptionsService) : AbstractAutomaticLineEnderCommandHandler(undoRegistry, editorOperations, editorOptionsService)
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

        protected override void NextAction(IEditorOperations editorOperation, Action nextAction)
            => editorOperation.InsertNewLine();

        protected override bool TreatAsReturn(ParsedDocument document, int caretPosition, CancellationToken cancellationToken)
        {
            var endToken = document.Root.FindToken(caretPosition);
            if (endToken.IsMissing)
            {
                return false;
            }

            var tokenToLeft = document.Root.FindTokenOnLeftOfPosition(caretPosition);
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

        protected override IList<TextChange> FormatBasedOnEndToken(ParsedDocument document, int position, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        {
            var root = document.Root;
            var endToken = root.FindToken(position);
            var span = GetFormattedTextSpan(root, endToken);
            if (span == null)
            {
                return SpecializedCollections.EmptyList<TextChange>();
            }

            var formatter = document.LanguageServices.GetRequiredService<ISyntaxFormattingService>();
            return formatter.GetFormattingResult(
                root,
                SpecializedCollections.SingletonCollection(CommonFormattingHelpers.GetFormattingSpan(root, span.Value)),
                options,
                rules: null,
                cancellationToken).GetTextChanges(cancellationToken);
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

        protected override string? GetEndingString(ParsedDocument document, int position)
        {
            var root = document.Root;
            var text = document.Text;
            var tree = document.SyntaxTree;

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
            if (owningNode is UsingDirectiveSyntax u && u.NamespaceOrType.IsMissing)
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
            ParsedDocument document,
            SyntaxNode selectedNode,
            bool addBrace,
            int caretPosition,
            CancellationToken cancellationToken)
        {
            var formattingOptions = args.SubjectBuffer.GetSyntaxFormattingOptions(EditorOptionsService, document.LanguageServices, explicitFormat: false);

            // Add braces for the selected node
            if (addBrace)
            {
                // For these syntax node, braces pair could be easily added by modify the syntax tree
                if (selectedNode is BaseTypeDeclarationSyntax
                    or BaseMethodDeclarationSyntax
                    or LocalFunctionStatementSyntax
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
                    var (newRoot, nextCaretPosition) = AddBraceToSelectedNode(document.SolutionServices, document.Root, selectedNode, formattingOptions, cancellationToken);

                    var newDocument = document.WithChangedRoot(newRoot, cancellationToken);
                    args.SubjectBuffer.ApplyChanges(newDocument.GetChanges(document));
                    args.TextView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(args.SubjectBuffer.CurrentSnapshot, nextCaretPosition));
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
                    // For field and event declarations node because
                    // class A
                    // {
                    //     int Hel$$lo
                    //
                    //     [SomeAttribute]
                    //     void Bar() { }
                    // }
                    // Parser would think '[SomeAttribute]' (BrackedArgumentList) is a part of the 'Hello' (VariableDeclarator).
                    // So for these cases, just find the missing open brace position and directly insert '()' to the document

                    // 1. Find the position to insert braces.
                    var insertionPosition = GetBraceInsertionPosition(selectedNode);

                    // 2. Insert the braces and move caret
                    InsertBraceAndMoveCaret(args.TextView, args.SubjectBuffer, document, formattingOptions, insertionPosition, cancellationToken);
                }
            }
            else
            {
                // Remove the braces and get the next caretPosition
                var (newRoot, nextCaretPosition) = RemoveBraceFromSelectedNode(
                    document.SolutionServices,
                    document.Root,
                    selectedNode,
                    formattingOptions,
                    cancellationToken);

                var newDocument = document.WithChangedRoot(newRoot, cancellationToken);
                args.SubjectBuffer.ApplyChanges(newDocument.GetChanges(document));
                args.TextView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(args.SubjectBuffer.CurrentSnapshot, nextCaretPosition));
            }
        }

        private static (SyntaxNode newRoot, int nextCaretPosition) AddBraceToSelectedNode(
            SolutionServices services,
            SyntaxNode root,
            SyntaxNode selectedNode,
            SyntaxFormattingOptions formattingOptions,
            CancellationToken cancellationToken)
        {
            // For these nodes, directly modify the node and replace it.
            if (selectedNode is BaseTypeDeclarationSyntax
                or BaseMethodDeclarationSyntax
                or LocalFunctionStatementSyntax
                or AccessorDeclarationSyntax)
            {
                var newRoot = ReplaceNodeAndFormat(
                    services,
                    root,
                    selectedNode,
                    WithBraces(selectedNode, formattingOptions),
                    formattingOptions,
                    cancellationToken);
                // Locate the open brace token, and move the caret after it.
                var nextCaretPosition = GetOpenBraceSpanEnd(newRoot);
                return (newRoot, nextCaretPosition);
            }

            // For ObjectCreationExpression, like new List<int>()
            // It requires
            // 1. Add an initializer to it.
            // 2. make sure it has '()' after the type, and if its next token is a missing semicolon, add that semicolon. e.g
            // var c = new Obje$$ct() => var c = new Object();
            if (selectedNode is ObjectCreationExpressionSyntax objectCreationExpressionNode)
            {
                var (newNode, oldNode) = ModifyObjectCreationExpressionNode(objectCreationExpressionNode, addOrRemoveInitializer: true, formattingOptions);
                var newRoot = ReplaceNodeAndFormat(
                    services,
                    root,
                    oldNode,
                    newNode,
                    formattingOptions,
                    cancellationToken);

                // Locate the open brace token, and move the caret after it.
                var nextCaretPosition = GetOpenBraceSpanEnd(newRoot);
                return (newRoot, nextCaretPosition);
            }

            // For the embeddedStatementOwner node, like ifStatement/elseClause
            // It requires:
            // 1. Add a empty block as its statement.
            // 2. Handle its previous statement if needed.
            // case 1:
            // if$$ (true)
            // var c = 10;
            // =>
            // if (true)
            // {
            //     $$
            // }
            // var c = 10;
            // In this case, 'var c = 10;' is considered as the inner statement so we need to move it next to the if Statement
            //
            // case 2:
            // if (true)
            // {
            // }
            // else if$$ (false)
            //    Print("Bar");
            // else
            // {
            // }
            // =>
            // if (true)
            // {
            // }
            // else if (false)
            // {
            //    $$
            //    Print("Bar");
            // }
            // else
            // {
            // }
            // In this case 'Print("Bar")' is considered as the innerStatement so when we inserted the empty block, we need also insert that
            if (selectedNode.IsEmbeddedStatementOwner())
            {
                return AddBraceToEmbeddedStatementOwner(services, root, selectedNode, formattingOptions, cancellationToken);
            }

            throw ExceptionUtilities.UnexpectedValue(selectedNode);
        }

        private static (SyntaxNode newRoot, int nextCaretPosition) RemoveBraceFromSelectedNode(
            SolutionServices services,
            SyntaxNode root,
            SyntaxNode selectedNode,
            SyntaxFormattingOptions formattingOptions,
            CancellationToken cancellationToken)
        {
            // Remove the initializer from ObjectCreationExpression
            // Step 1. Remove the initializer
            // e.g. var c = new Bar { $$ } => var c = new Bar
            //
            // Step 2. Add parenthesis
            // e.g var c = new Bar => var c = new Bar()
            //
            // Step 3. Add semicolon if needed
            // e.g. var c = new Bar() => var c = new Bar();
            if (selectedNode is BaseObjectCreationExpressionSyntax objectCreationExpressionNode)
            {
                var (newNode, oldNode) = ModifyObjectCreationExpressionNode(objectCreationExpressionNode, addOrRemoveInitializer: false, formattingOptions);
                var newRoot = ReplaceNodeAndFormat(
                    services,
                    root,
                    oldNode,
                    newNode,
                    formattingOptions,
                    cancellationToken);

                // Find the replacement node, and move the caret to the end of line (where the last token is)
                var replacementNode = newRoot.GetAnnotatedNodes(s_replacementNodeAnnotation).Single();
                var lastToken = replacementNode.GetLastToken();
                var lineEnd = newRoot.GetText().Lines.GetLineFromPosition(lastToken.Span.End).End;
                return (newRoot, lineEnd);
            }
            else
            {
                // For all the other cases, include
                // 1. Property declaration => Field Declaration.
                //    e.g.
                // class Bar
                // {
                //      int Bar {$$}
                // }
                // =>
                // class Bar
                // {
                //     int Bar;
                // }
                // 2. Event Declaration => Event Field Declaration
                // class Bar
                // {
                //     event EventHandler e { $$ }
                // }
                // =>
                // class Bar
                // {
                //     event EventHandler e;
                // }
                // 3. Accessor
                // class Bar
                // {
                //     int Bar
                //     {
                //         get { $$ }
                //     }
                // }
                // =>
                // class Bar
                // {
                //     int Bar
                //     {
                //         get;
                //     }
                // }
                // Get its no-brace version of node and insert it into the root.
                var newRoot = ReplaceNodeAndFormat(
                    services,
                    root,
                    selectedNode,
                    WithoutBraces(selectedNode),
                    formattingOptions,
                    cancellationToken);

                // Locate the replacement node, move the caret to the end.
                // e.g.
                // class Bar
                // {
                //     event EventHandler e { $$ }
                // }
                // =>
                // class Bar
                // {
                //     event EventHandler e;$$
                // }
                // and we need to move the caret after semicolon
                var nextCaretPosition = newRoot.GetAnnotatedNodes(s_replacementNodeAnnotation).Single().GetLastToken().Span.End;
                return (newRoot, nextCaretPosition);
            }
        }

        private static int GetOpenBraceSpanEnd(SyntaxNode root)
        {
            // Use the annotation to find the end of the open brace.
            var annotatedOpenBraceToken = root.GetAnnotatedTokens(s_openBracePositionAnnotation).Single();
            return annotatedOpenBraceToken.Span.End;
        }

        private static int GetBraceInsertionPosition(SyntaxNode node)
        {
            if (node is SwitchStatementSyntax switchStatementNode)
            {
                // There is no parenthesis pair in the switchStatementNode, and the node before 'switch' is an expression
                // e.g.
                // void Foo(int i)
                // {
                //    var c = (i + 1) swit$$ch
                // }
                // Consider this as a SwitchExpression, add the brace after 'switch'
                if (switchStatementNode.OpenParenToken.IsMissing
                    && switchStatementNode.CloseParenToken.IsMissing
                    && IsTokenPartOfExpression(switchStatementNode.GetFirstToken().GetPreviousToken()))
                {
                    return switchStatementNode.SwitchKeyword.Span.End;
                }

                // In all other case, think it is a switch statement, add brace after the close parenthesis.
                return switchStatementNode.CloseParenToken.Span.End;
            }

            return node switch
            {
                NamespaceDeclarationSyntax => node.GetBraces().openBrace.SpanStart,
                IndexerDeclarationSyntax indexerNode => indexerNode.ParameterList.Span.End,
                TryStatementSyntax tryStatementNode => tryStatementNode.TryKeyword.Span.End,
                CatchClauseSyntax catchClauseNode => catchClauseNode.Block.SpanStart,
                FinallyClauseSyntax finallyClauseNode => finallyClauseNode.Block.SpanStart,
                CheckedStatementSyntax checkedStatementNode => checkedStatementNode.Keyword.Span.End,
                FieldDeclarationSyntax fieldDeclarationNode => fieldDeclarationNode.Declaration.Variables[0].Identifier.Span.End,
                EventFieldDeclarationSyntax eventFieldDeclarationNode => eventFieldDeclarationNode.Declaration.Variables[0].Identifier.Span.End,
                _ => throw ExceptionUtilities.Unreachable(),
            };
        }

        private static bool IsTokenPartOfExpression(SyntaxToken syntaxToken)
        {
            if (syntaxToken.IsMissing || syntaxToken.IsKind(SyntaxKind.None))
            {
                return false;
            }

            return !syntaxToken.GetAncestors<ExpressionSyntax>().IsEmpty();
        }

        private static string GetBracePairString(SyntaxFormattingOptions formattingOptions)
            => string.Concat(SyntaxFacts.GetText(SyntaxKind.OpenBraceToken),
                formattingOptions.NewLine,
                SyntaxFacts.GetText(SyntaxKind.CloseBraceToken));

        private void InsertBraceAndMoveCaret(
            ITextView textView,
            ITextBuffer buffer,
            ParsedDocument document,
            SyntaxFormattingOptions formattingOptions,
            int insertionPosition,
            CancellationToken cancellationToken)
        {
            var bracePair = GetBracePairString(formattingOptions);

            // 1. Insert { }.
            var insertChange = new TextChange(new TextSpan(insertionPosition, 0), bracePair);
            buffer.ApplyChange(insertChange);
            var newDocument = document.WithChange(insertChange, cancellationToken);

            // 2. Place caret between the braces.
            textView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(textView.TextSnapshot, insertionPosition + 1));

            // 3. Format the document using the close brace.
            var changes = FormatBasedOnEndToken(newDocument, insertionPosition + bracePair.Length - 1, formattingOptions, cancellationToken);
            buffer.ApplyChanges(changes);
        }

        protected override (SyntaxNode selectedNode, bool addBrace)? GetValidNodeToModifyBraces(ParsedDocument document, int caretPosition, CancellationToken cancellationToken)
        {
            var token = document.Root.FindTokenOnLeftOfPosition(caretPosition);
            if (token.IsKind(SyntaxKind.None))
            {
                return null;
            }

            foreach (var node in token.GetAncestors<SyntaxNode>())
            {
                if (ShouldAddBraces(node, caretPosition))
                {
                    return (selectedNode: node, addBrace: true);
                }

                if (ShouldRemoveBraces(node, caretPosition))
                {
                    return (selectedNode: node, addBrace: false);
                }
            }

            return null;
        }

        #endregion
    }
}
