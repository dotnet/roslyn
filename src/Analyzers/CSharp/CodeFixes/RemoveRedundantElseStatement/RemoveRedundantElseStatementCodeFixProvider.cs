// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveRedundantElseStatement;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveRedundantElseStatement), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RemoveRedundantElseStatementCodeFixProvider()
    : ForkingSyntaxEditorBasedCodeFixProvider<IfStatementSyntax>(
        CSharpAnalyzersResources.Remove_redundant_else_statement,
        nameof(CSharpAnalyzersResources.Remove_redundant_else_statement))
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(IDEDiagnosticIds.RemoveRedundantElseStatementDiagnosticId);

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        IfStatementSyntax ifStatement,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (!RemoveRedundantElseStatementDiagnosticAnalyzer.CanSimplify(semanticModel, ifStatement, out var elseClause, cancellationToken))
            return;

        var options = await document.GetCodeFixOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
        var formattingOptions = options.GetFormattingOptions(CSharpSyntaxFormatting.Instance);

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var ifIndentation = GetIndentationStringForPosition(ifStatement.IfKeyword.SpanStart);
        var globalStatement = ifStatement.Parent as GlobalStatementSyntax;
        var newLineTrivia = EndOfLine(formattingOptions.NewLine);

        // Cases to consider:
        // 
        //  1. No block.  Embedded statement on same line
        //
        //      else EmbeddedStatement()
        //
        //  2. No block.  EmbeddedStatement follows
        //
        //      else
        //          EmbeddedStatement()
        //
        //  3. Block.  Single line after else:
        //
        //      else { EmbeddedStatement(); }
        //
        //  4. Block. After else:
        //
        //      else
        //      {
        //          ...
        //      }

        // Always remove the else clause.
        editor.RemoveNode(elseClause);

        var statementsToRewrite = elseClause.Statement is BlockSyntax elseBlock
            ? elseBlock.Statements
            : SingletonList(elseClause.Statement);

        // Now go through all the statements, adjust their indentation outwards as appropriate, then add a blank line if
        // the if-statement ends with a `}`.
        var rewrittenStatements = AddBlankLineIfMissing(UpdateIndentation(statementsToRewrite, ifIndentation));

        // Insert these statements after the `if` statement/
        editor.InsertAfter(
             globalStatement ?? (SyntaxNode)ifStatement,
             WrapWithGlobalStatements(rewrittenStatements));

        // Finally, if we have `if () { } else { }` then trim the trailing whitespace at the end of the if's block.
        var elseToken = elseClause.ElseKeyword;
        var beforeElseToken = elseToken.GetPreviousToken();
        if (text.AreOnSameLine(beforeElseToken, elseToken) &&
            beforeElseToken.TrailingTrivia.All(t => t.IsWhitespace()))
        {
            var beforeElseParent = beforeElseToken.GetRequiredParent();
            editor.ReplaceNode(
                beforeElseParent,
                beforeElseParent.ReplaceToken(beforeElseToken, beforeElseToken.WithTrailingTrivia(newLineTrivia)));
        }

        return;

        IEnumerable<SyntaxNode> WrapWithGlobalStatements(IEnumerable<StatementSyntax> statements)
            => statements.Select(WrapWithGlobalStatement);

        SyntaxNode WrapWithGlobalStatement(StatementSyntax statement)
            => globalStatement != null ? GlobalStatement(statement) : statement;

        IEnumerable<StatementSyntax> AddBlankLineIfMissing(IEnumerable<StatementSyntax> statements)
        {
            var first = true;
            foreach (var statement in statements)
            {
                if (first &&
                    ifStatement.Statement is BlockSyntax &&
                    statement.GetLeadingTrivia() is not [(kind: SyntaxKind.EndOfLineTrivia), ..])
                {
                    yield return statement.WithPrependedLeadingTrivia(newLineTrivia);
                }
                else
                {
                    yield return statement;
                }

                first = false;
            }
        }

        IEnumerable<StatementSyntax> UpdateIndentation(SyntaxList<StatementSyntax> statements, string ifIndentation)
        {
            for (var i = 0; i < statements.Count; i++)
            {
                var statement = statements[i];

                var statementFirstToken = statement.GetFirstToken();
                var onSameLineAsPrevious = text.AreOnSameLine(statementFirstToken.GetPreviousToken(), statementFirstToken);

                if (onSameLineAsPrevious)
                {
                    // else { EmbeddedStatement(); }
                    //
                    // Place on new line at the appropriate indentation.
                    if (i == 0)
                    {
                        yield return statement.WithPrependedLeadingTrivia(newLineTrivia, Whitespace(ifIndentation));
                    }
                    else
                    {
                        // else { X(); Y(); }
                        //
                        // For successive statements, don't touch.  We want to preserve where it is in the outer scope.
                        yield return statement;
                    }
                }
                else
                {
                    yield return AdjustIndentation(statement, ifIndentation);
                }
            }
        }

        StatementSyntax AdjustIndentation(StatementSyntax statement, string ifIndentation)
        {
            var firstTokenOnLineIndentationString = GetIndentationStringForToken(statement.GetFirstToken());
            if (!firstTokenOnLineIndentationString.StartsWith(ifIndentation))
                return statement;

            var indentationToTrim = firstTokenOnLineIndentationString.Substring(ifIndentation.Length);
            if (indentationToTrim.Length == 0)
                return statement;

            var statementFirstToken = statement.GetFirstToken();
            var updatedStatement = statement.ReplaceTokens(
                statement.DescendantTokens(),
                (currentToken, _) => DedentToken(currentToken, indentationToTrim, force: currentToken == statementFirstToken));

            return updatedStatement;
        }

        SyntaxToken DedentToken(
            SyntaxToken token,
            string indentationToTrim,
            bool force)
        {
            // If a token has any leading whitespace, it must be at the start of a line.  Whitespace is otherwise always
            // consumed as trailing trivia if it comes after a token.
            if (!force && token.LeadingTrivia is not [.., (kind: SyntaxKind.WhitespaceTrivia)])
                return token;

            using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(out var result);

            // Walk all trivia (except the final whitespace).  If we hit any comments within at the start of a line
            // dedent them as well.
            for (int i = 0, n = token.LeadingTrivia.Count - 1; i < n; i++)
            {
                var currentTrivia = token.LeadingTrivia[i];
                var nextTrivia = token.LeadingTrivia[i + 1];

                var afterNewLine = i == 0 || token.LeadingTrivia[i - 1].IsEndOfLine();
                if (afterNewLine &&
                    currentTrivia.IsWhitespace() &&
                    nextTrivia.IsSingleOrMultiLineComment())
                {
                    result.Add(GetIndentedWhitespaceTrivia(indentationToTrim, nextTrivia.SpanStart));
                }
                else
                {
                    result.Add(currentTrivia);
                }
            }

            // Finally, figure out how much this token is indented, and if that indent includes the amount we want to
            // dedent.  If so, dedent accordingly.
            result.Add(GetIndentedWhitespaceTrivia(indentationToTrim, token.SpanStart));

            return token.WithLeadingTrivia(TriviaList(result));
        }

        SyntaxTrivia GetIndentedWhitespaceTrivia(string indentationToTrim, int pos)
        {
            var positionIndentation = GetIndentationStringForPosition(pos);
            return positionIndentation.EndsWith(indentationToTrim)
                ? Whitespace(positionIndentation[0..^indentationToTrim.Length])
                : Whitespace(positionIndentation);
        }

        string GetIndentationStringForToken(SyntaxToken token)
            => GetIndentationStringForPosition(token.SpanStart);

        string GetIndentationStringForPosition(int position)
        {
            var lineContainingPosition = text.Lines.GetLineFromPosition(position);
            var lineText = lineContainingPosition.ToString();
            var indentation = lineText.ConvertTabToSpace(formattingOptions.TabSize, initialColumn: 0, endPosition: position - lineContainingPosition.Start);
            return indentation.CreateIndentationString(formattingOptions.UseTabs, formattingOptions.TabSize);
        }
    }
}
