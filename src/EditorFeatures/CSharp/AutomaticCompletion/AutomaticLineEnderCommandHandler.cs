// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion
{
    /// <summary>
    /// csharp automatic line ender command handler
    /// </summary>
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.AutomaticLineEnder)]
    [Order(After = PredefinedCommandHandlerNames.Completion)]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal class AutomaticLineEnderCommandHandler : AbstractAutomaticLineEnderCommandHandler
    {
        [ImportingConstructor]
        public AutomaticLineEnderCommandHandler(
            ITextUndoHistoryRegistry undoRegistry,
            IEditorOperationsFactoryService editorOperations)
            : base(undoRegistry, editorOperations)
        {
        }

        protected override void NextAction(IEditorOperations editorOperation, Action nextAction)
        {
            editorOperation.InsertNewLine();
        }

        protected override bool TreatAsReturn(Document document, int position, CancellationToken cancellationToken)
        {
            var root = document.GetSyntaxRootSynchronously(cancellationToken);

            var endToken = root.FindToken(position);
            if (endToken.IsMissing)
            {
                return false;
            }

            var tokenToLeft = root.FindTokenOnLeftOfPosition(position);
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

        protected override void FormatAndApply(Document document, int position, CancellationToken cancellationToken)
        {
            var root = document.GetSyntaxRootSynchronously(cancellationToken);

            var endToken = root.FindToken(position);
            if (endToken.IsMissing)
            {
                return;
            }

            var ranges = FormattingRangeHelper.FindAppropriateRange(endToken, useDefaultRange: false);
            if (ranges == null)
            {
                return;
            }

            var startToken = ranges.Value.Item1;
            if (startToken.IsMissing || startToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            var options = document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            var changes = Formatter.GetFormattedTextChanges(root, new TextSpan[] { TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End) }, document.Project.Solution.Workspace, options,
                rules: null, // use default
                cancellationToken: cancellationToken);

            document.ApplyTextChanges(changes.ToArray(), cancellationToken);
        }

        protected override string GetEndingString(Document document, int position, CancellationToken cancellationToken)
        {
            // prepare expansive information from document
            var tree = document.GetSyntaxTreeSynchronously(cancellationToken);
            var root = tree.GetRoot(cancellationToken);
            var text = tree.GetText(cancellationToken);
            var semicolon = SyntaxFacts.GetText(SyntaxKind.SemicolonToken);

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
                var textToParse = owningNode.NormalizeWhitespace().ToFullString() + semicolon;

                // currently, Parsing a field is not supported. as a workaround, wrap the field in a type and parse
                var node = ParseNode(tree, owningNode, textToParse);

                // Insert line ender if we didn't introduce any diagnostics, if not try the next owning node.
                if (node != null && !node.ContainsDiagnostics)
                {
                    return semicolon;
                }
            }

            return null;
        }

        private SyntaxNode ParseNode(SyntaxTree tree, SyntaxNode owningNode, string textToParse)
            => owningNode switch
            {
                BaseFieldDeclarationSyntax n => SyntaxFactory.ParseCompilationUnit(WrapInType(textToParse), options: (CSharpParseOptions)tree.Options),
                BaseMethodDeclarationSyntax n => SyntaxFactory.ParseCompilationUnit(WrapInType(textToParse), options: (CSharpParseOptions)tree.Options),
                BasePropertyDeclarationSyntax n => SyntaxFactory.ParseCompilationUnit(WrapInType(textToParse), options: (CSharpParseOptions)tree.Options),
                StatementSyntax n => SyntaxFactory.ParseStatement(textToParse, options: (CSharpParseOptions)tree.Options),
                UsingDirectiveSyntax n => SyntaxFactory.ParseCompilationUnit(textToParse, options: (CSharpParseOptions)tree.Options),

                _ => (SyntaxNode)null,
            };

        /// <summary>
        /// wrap field in type
        /// </summary>
        private string WrapInType(string textToParse)
        {
            return "class C { " + textToParse + " }";
        }

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
            if (owningNode is UsingDirectiveSyntax u &&
                (u.Name == null || u.Name.IsMissing))
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
        {
            return lastToken.IsMissing && lastToken.Span.End == line.EndIncludingLineBreak;
        }

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
                        .Select(OwningNode);
        }

        private static bool AllowedConstructs(SyntaxNode n)
        {
            return n is StatementSyntax ||
                   n is BaseFieldDeclarationSyntax ||
                   n is UsingDirectiveSyntax ||
                   n is ArrowExpressionClauseSyntax;
        }

        private static SyntaxNode OwningNode(SyntaxNode n)
        {
            return n is ArrowExpressionClauseSyntax ? n.Parent : n;
        }
    }
}
