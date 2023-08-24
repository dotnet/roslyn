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
        editor.RemoveNode(elseClause);
        if (elseClause.Statement is BlockSyntax elseBlock)
        {
            editor.InsertAfter(
                globalStatement ?? (SyntaxNode)ifStatement,
                WrapWithGlobalStatements(UpdateIndentation(elseBlock.Statements, ifIndentation)));
        }
        else
        {
            // One of the following forms:
            //
            //  if ... else ...
            //
            //  if ...
            //  else ...
            //
            //  if
            //      ...
            //  else
            //      ...

            var elseStatement = elseClause.Statement;
            var elseStatementFirstToken = elseStatement.GetFirstToken();

            StatementSyntax newStatement;
            if (text.AreOnSameLine(elseStatementFirstToken.GetPreviousToken(), elseStatementFirstToken))
            {
                newStatement = elseStatement.WithPrependedLeadingTrivia(EndOfLine(formattingOptions.NewLine), Whitespace(ifIndentation));
            }
            else
            {
                newStatement = AdjustIndentation(elseStatement, ifIndentation);
            }

            editor.InsertAfter(
                globalStatement ?? (SyntaxNode)ifStatement,
                WrapWithGlobalStatement(newStatement));
        }

        return;

        IEnumerable<SyntaxNode> WrapWithGlobalStatements(IEnumerable<StatementSyntax> statements)
            => statements.Select(WrapWithGlobalStatement);

        SyntaxNode WrapWithGlobalStatement(StatementSyntax statement)
            => globalStatement != null ? GlobalStatement(statement) : statement;

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
                        yield return statement.WithPrependedLeadingTrivia(EndOfLine(formattingOptions.NewLine), Whitespace(ifIndentation));
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
                (currentToken, _) =>
                {
                    // Ensure the first token has the indentation we're moving the entire node to
                    return DedentToken(currentToken, indentationToTrim, force: currentToken == statementFirstToken);
                });

            return updatedStatement;
        }



        SyntaxToken DedentToken(
            SyntaxToken token,
            string indentationToTrim,
            bool force)
        {
            // If a token has any leading whitespace, it must be at the start of a line.  Whitespace is
            // otherwise always consumed as trailing trivia if it comes after a token.
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


#if false
    protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var ifStatements = diagnostics
            .Select(diagnostic => diagnostic.AdditionalLocations[0].FindNode(cancellationToken))
            .ToSet();

        var nodesToUpdate = ifStatements.Select(statement => statement.GetRequiredParent());

        var root = editor.OriginalRoot;

        var updatedRoot = root.ReplaceNodes(
            nodesToUpdate,
            (original, current) => original switch
            {
                CompilationUnitSyntax compilationUnit => RewriteCompilationUnit(compilationUnit, (CompilationUnitSyntax)current, ifStatements),
                _ => RewriteNode(original, current, ifStatements),
            }
        );

        editor.ReplaceNode(root, updatedRoot);

        return Task.CompletedTask;
    }

    private static CompilationUnitSyntax RewriteCompilationUnit(CompilationUnitSyntax original, CompilationUnitSyntax current, ISet<SyntaxNode> ifStatements)
    {
        var memberToUpdateIndex = original.Members.IndexOf(ifStatements.Contains);
        var memberToUpdate = (GlobalStatementSyntax)original.Members[memberToUpdateIndex];

        var elseClause = GetLastElse((IfStatementSyntax)memberToUpdate.Statement);
        var ifWithoutElse = memberToUpdate
            .RemoveNode(elseClause, SyntaxRemoveOptions.KeepExteriorTrivia)
            !.WithAppendedTrailingTrivia(CarriageReturnLineFeed);

        var updatedCompilationUnit = current.ReplaceNode(memberToUpdate, ifWithoutElse);
        var newStatements = Expand(elseClause).Select(GlobalStatement);

        var updatedMembers = updatedCompilationUnit.Members
            .InsertRange(memberToUpdateIndex + 1, newStatements);

        return current.WithMembers(updatedMembers);
    }

    private static SyntaxNode RewriteNode(SyntaxNode original, SyntaxNode current, ISet<SyntaxNode> ifStatements)
    {
        var originalNodeStatements = GetStatements(original);
        var currentNodeStatements = GetStatements(current);

        var statementToUpdateIndex = originalNodeStatements.IndexOf(ifStatements.Contains);
        var statementToUpdate = currentNodeStatements[statementToUpdateIndex];

        var elseClause = GetLastElse((IfStatementSyntax)statementToUpdate);
        var ifWithoutElse = statementToUpdate.RemoveNode(elseClause, SyntaxRemoveOptions.KeepEndOfLine);
        var newStatements = new[] { ifWithoutElse }.Concat(Expand(elseClause));

        var updatedStatements = currentNodeStatements.ReplaceRange(statementToUpdate, newStatements!);

        return current switch
        {
            BlockSyntax block => block.WithStatements(updatedStatements),
            SwitchSectionSyntax switchSelection => switchSelection.WithStatements(updatedStatements),
            _ => throw ExceptionUtilities.UnexpectedValue(current.Kind())
        };
    }

    private static SyntaxList<StatementSyntax> GetStatements(SyntaxNode node)
    {
        return node switch
        {
            BlockSyntax block => block.Statements,
            SwitchSectionSyntax switchSelection => switchSelection.Statements,
            _ => throw ExceptionUtilities.UnexpectedValue(node.Kind())
        };
    }

    private static ElseClauseSyntax GetLastElse(IfStatementSyntax ifStatement)
    {
        while (ifStatement.Else?.Statement is IfStatementSyntax elseIfStatement)
        {
            ifStatement = elseIfStatement;
        }

        return ifStatement.Else!;
    }

    private static ImmutableArray<StatementSyntax> Expand(ElseClauseSyntax elseClause)
    {
        using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var result);

        switch (elseClause.Statement)
        {
            case BlockSyntax block:
                result.AddRange(block.Statements);
                break;

            case StatementSyntax anythingElse:
                result.Add(anythingElse);
                break;
        }

        return result
            .Select(statement => statement.WithAdditionalAnnotations(Formatter.Annotation))
            .AsImmutable();
    }
#endif
}
