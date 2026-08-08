// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Indentation;

internal partial class CSharpIndentationService
{
    protected override bool ShouldUseTokenIndenter(Indenter indenter, out SyntaxToken syntaxToken)
        => ShouldUseSmartTokenFormatterInsteadOfIndenter(
            indenter.Rules, indenter.Root, indenter.LineToBeIndented, indenter.Options, out syntaxToken);

    protected override ISmartTokenFormatter CreateSmartTokenFormatter(
        CompilationUnitSyntax root, SourceText text, TextLine lineToBeIndented,
        IndentationOptions options, AbstractFormattingRule baseIndentationRule)
    {
        return new CSharpSmartTokenFormatter(
            options, [baseIndentationRule, .. CSharpSyntaxFormatting.Instance.GetDefaultFormattingRules()], root, text);
    }

    protected override IndentationResult? GetDesiredIndentationWorker(Indenter indenter, SyntaxToken? tokenOpt, SyntaxTrivia? triviaOpt)
        => TryGetDesiredIndentation(indenter, triviaOpt) ??
           TryGetDesiredIndentation(indenter, tokenOpt);

    private static IndentationResult? TryGetDesiredIndentation(Indenter indenter, SyntaxTrivia? triviaOpt)
    {
        // If we have a // comment, and it's the only thing on the line, then if we hit enter, we should align to
        // that.  This helps for cases like:
        //
        //          int goo; // this comment
        //                   // continues
        //                   // onwards
        //
        // The user will have to manually indent `// continues`, but we'll respect that indentation from that point on.

        if (triviaOpt == null)
            return null;

        var trivia = triviaOpt.Value;
        if (!trivia.IsSingleOrMultiLineComment() && !trivia.IsDocComment())
            return null;

        var line = indenter.Text.Lines.GetLineFromPosition(trivia.FullSpan.Start);
        if (line.GetFirstNonWhitespacePosition() != trivia.FullSpan.Start)
            return null;

        // Previous line just contained this single line comment.  Align us with it.
        return new IndentationResult(trivia.FullSpan.Start, 0);
    }

    private static IndentationResult? TryGetDesiredIndentation(Indenter indenter, SyntaxToken? tokenOpt)
    {
        if (tokenOpt == null)
            return null;

        return GetIndentationBasedOnToken(indenter, tokenOpt.Value);
    }

    private static IndentationResult GetIndentationBasedOnToken(Indenter indenter, SyntaxToken token)
    {
        Contract.ThrowIfNull(indenter.Tree);
        Contract.ThrowIfTrue(token.Kind() == SyntaxKind.None);

        var sourceText = indenter.LineToBeIndented.Text;
        RoslynDebug.AssertNotNull(sourceText);

        // case: """$$
        //       """
        if (token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
        {
            var endLine = sourceText.Lines.GetLineFromPosition(token.Span.End);

            // Raw string may be unterminated.  So last line may just be the last line of the file, which may have
            // no contents on it.  In that case, just presume the minimum offset is 0.
            var minimumOffset = endLine.GetFirstNonWhitespaceOffset() ?? 0;

            // If possible, indent to match the indentation of the previous non-whitespace line contained in the
            // same raw string. Otherwise, indent to match the ending line of the raw string.
            var startLine = sourceText.Lines.GetLineFromPosition(token.SpanStart);
            for (var currentLineNumber = indenter.LineToBeIndented.LineNumber - 1; currentLineNumber >= startLine.LineNumber + 1; currentLineNumber--)
            {
                var currentLine = sourceText.Lines[currentLineNumber];
                if (currentLine.GetFirstNonWhitespaceOffset() is { } priorLineOffset)
                {
                    if (priorLineOffset >= minimumOffset)
                    {
                        return indenter.GetIndentationOfLine(currentLine);
                    }
                    else
                    {
                        // The prior line is not sufficiently indented, so use the ending delimiter for the indent
                        break;
                    }
                }
            }

            return indenter.GetIndentationOfLine(endLine);
        }

        // case 1: $"""$$
        //          """
        // case 2: $"""
        //          text$$
        //          """
        // case 3: $"""
        //          {value}$$
        //          """
        if (token.Kind() is SyntaxKind.InterpolatedMultiLineRawStringStartToken or SyntaxKind.InterpolatedStringTextToken
            || token is { RawKind: (int)SyntaxKind.CloseBraceToken, Parent: InterpolationSyntax })
        {
            var interpolatedExpression = token.GetAncestor<InterpolatedStringExpressionSyntax>();
            Contract.ThrowIfNull(interpolatedExpression);
            if (interpolatedExpression.StringStartToken.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken))
            {
                var endLine = sourceText.Lines.GetLineFromPosition(interpolatedExpression.StringEndToken.Span.End);

                // Raw string may be unterminated.  So last line may just be the last line of the file, which may have
                // no contents on it.  In that case, just presume the minimum offset is 0.
                var minimumOffset = endLine.GetFirstNonWhitespaceOffset() ?? 0;

                // If possible, indent to match the indentation of the previous non-whitespace line contained in the
                // same raw string. Otherwise, indent to match the ending line of the raw string.
                var startLine = sourceText.Lines.GetLineFromPosition(interpolatedExpression.StringStartToken.SpanStart);
                for (var currentLineNumber = indenter.LineToBeIndented.LineNumber - 1; currentLineNumber >= startLine.LineNumber + 1; currentLineNumber--)
                {
                    var currentLine = sourceText.Lines[currentLineNumber];
                    if (!indenter.Root.FindToken(currentLine.Start, findInsideTrivia: true).IsKind(SyntaxKind.InterpolatedStringTextToken))
                    {
                        // Avoid trying to indent to match the content of an interpolation. Example:
                        //
                        // _ = $"""
                        //     {
                        //  0}         <-- the start of this line is not part of the text content
                        //     """
                        //
                        continue;
                    }

                    if (currentLine.GetFirstNonWhitespaceOffset() is { } priorLineOffset)
                    {
                        if (priorLineOffset >= minimumOffset)
                        {
                            return indenter.GetIndentationOfLine(currentLine);
                        }
                        else
                        {
                            // The prior line is not sufficiently indented, so use the ending delimiter for the indent
                            break;
                        }
                    }
                }

                return indenter.GetIndentationOfLine(endLine);
            }
        }

        // special cases
        // case 1: token belongs to verbatim token literal
        // case 2: $@"$${0}"
        // case 3: $@"Comment$$ in-between{0}"
        // case 4: $@"{0}$$"
        if (token.IsVerbatimStringLiteral() ||
            token.Kind() is SyntaxKind.InterpolatedVerbatimStringStartToken or SyntaxKind.InterpolatedStringTextToken ||
            (token.IsKind(SyntaxKind.CloseBraceToken) && token.Parent.IsKind(SyntaxKind.Interpolation)))
        {
            return indenter.IndentFromStartOfLine(0);
        }

        // if previous statement belong to labeled statement, don't follow label's indentation
        // but its previous one.
        if (token.Parent is LabeledStatementSyntax || token.IsLastTokenInLabelStatement())
        {
            token = token.GetAncestor<LabeledStatementSyntax>()!.GetFirstToken(includeZeroWidth: true).GetPreviousToken(includeZeroWidth: true);
        }

        var position = indenter.GetCurrentPositionNotBelongToEndOfFileToken(indenter.LineToBeIndented.Start);

        // first check operation service to see whether we can determine indentation from it
        var indentation = indenter.Finder.FromIndentBlockOperations(indenter.Tree, token, position, indenter.CancellationToken);
        if (indentation.HasValue)
        {
            return indenter.IndentFromStartOfLine(indentation.Value);
        }

        var alignmentTokenIndentation = indenter.Finder.FromAlignTokensOperations(indenter.Tree, token);
        if (alignmentTokenIndentation.HasValue)
        {
            return indenter.IndentFromStartOfLine(alignmentTokenIndentation.Value);
        }

        // if we couldn't determine indentation from the service, use heuristic to find indentation.

        // If this is the last token of an embedded statement, walk up to the top-most parenting embedded
        // statement owner and use its indentation.
        //
        // cases:
        //   if (true)
        //     if (false)
        //       Goo();
        //
        //   if (true)
        //     { }

        if (token.IsSemicolonOfEmbeddedStatement() ||
            token.IsCloseBraceOfEmbeddedBlock())
        {
            RoslynDebug.Assert(
                token.Parent?.Parent is StatementSyntax or ElseClauseSyntax);

            var embeddedStatementOwner = token.Parent.Parent;
            while (embeddedStatementOwner.IsEmbeddedStatement())
            {
                RoslynDebug.AssertNotNull(embeddedStatementOwner.Parent);

                // Don't walk up past a labeled statement, as we want to use the indentation of the
                // statement following the label, not the label itself (which may be at an arbitrary column).
                if (embeddedStatementOwner.Parent is LabeledStatementSyntax)
                    break;

                embeddedStatementOwner = embeddedStatementOwner.Parent;
            }

            // Match the indentation level of the outermost embedded statement owner.
            return indenter.GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(embeddedStatementOwner.GetFirstToken(includeZeroWidth: true).SpanStart));
        }

        var allowExtraIndentationLevel = true;

        switch (token.Kind())
        {
            case SyntaxKind.SemicolonToken:
                {
                    // special cases
                    if (!token.IsSemicolonInForStatement())
                    {
                        allowExtraIndentationLevel = false;
                    }

                    break;
                }

            case SyntaxKind.CloseBraceToken:
                {
                    if (token.Parent.IsKind(SyntaxKind.AccessorList) &&
                        token.Parent.Parent.IsKind(SyntaxKind.PropertyDeclaration))
                    {
                        if (token.GetNextToken().IsEqualsTokenInAutoPropertyInitializers())
                        {
                            break;
                        }
                    }

                    allowExtraIndentationLevel = false;
                    break;
                }

            case SyntaxKind.OpenBraceToken:
                {
                    allowExtraIndentationLevel = false;
                    break;
                }

            case SyntaxKind.ColonToken:
                {
                    var nonTerminalNode = token.Parent;
                    Contract.ThrowIfNull(nonTerminalNode, @"Malformed code or bug in parser???");

                    if (nonTerminalNode is SwitchLabelSyntax)
                    {
                        // Match the indentation level on the line starting the switch, then add an extra level of indentation.
                        return indenter.GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(nonTerminalNode.GetFirstToken(includeZeroWidth: true).SpanStart), indenter.Options.FormattingOptions.IndentationSize);
                    }

                    break;
                }

            case SyntaxKind.CloseBracketToken:
                {
                    var nonTerminalNode = token.Parent;
                    Contract.ThrowIfNull(nonTerminalNode, @"Malformed code or bug in parser???");

                    // if this is closing an attribute, we shouldn't indent.
                    if (nonTerminalNode is AttributeListSyntax)
                    {
                        // Match the indentation level of the line starting the attribute.
                        return indenter.GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(nonTerminalNode.GetFirstToken(includeZeroWidth: true).SpanStart));
                    }

                    break;
                }

            case SyntaxKind.XmlTextLiteralToken:
                {
                    // Match the indentation level of the line starting the XML doc comment.
                    return indenter.GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(token.SpanStart));
                }

            case SyntaxKind.CommaToken:
                {
                    if (TryGetIndentationFromCommaSeparatedList(indenter, token) is { } commaResult)
                    {
                        return commaResult;
                    }

                    break;
                }

            case SyntaxKind.CloseParenToken:
                {
                    if (token.Parent.IsKind(SyntaxKind.ArgumentList))
                    {
                        token = token.Parent.GetFirstToken(includeZeroWidth: true);
                    }

                    break;
                }
        }

        var result = TryGetIndentationForQueryExpression(indenter, token)
            ?? TryGetIndentationFromMultilineStatement(indenter, token);

        if (result is not null)
        {
            return result.Value;
        }

        // Apply indentation from format rules' IndentBlockOperations, possibly adding an extra level of indentation for
        // wrapping. This is only added if a relative IndentBlockOperation is not selected.
        var extraSpaces = allowExtraIndentationLevel ? indenter.Options.FormattingOptions.IndentationSize : 0;

        return indenter.IndentFromStartOfLine(indenter.Finder.GetIndentationOfCurrentPosition(indenter.Tree, token, position, extraSpaces, indenter.CancellationToken));
    }

    private static IndentationResult? TryGetIndentationFromCommaSeparatedList(Indenter indenter, SyntaxToken token)
        => token.Parent switch
        {
            BaseArgumentListSyntax argument => TryGetIndentationFromCommaSeparatedList(indenter, argument.Arguments, token),
            BaseParameterListSyntax parameter => TryGetIndentationFromCommaSeparatedList(indenter, parameter.Parameters, token),
            TypeArgumentListSyntax typeArgument => TryGetIndentationFromCommaSeparatedList(indenter, typeArgument.Arguments, token),
            TypeParameterListSyntax typeParameter => TryGetIndentationFromCommaSeparatedList(indenter, typeParameter.Parameters, token),
            EnumDeclarationSyntax enumDeclaration => TryGetIndentationFromCommaSeparatedList(indenter, enumDeclaration.Members, token),
            InitializerExpressionSyntax initializerSyntax => TryGetIndentationFromCommaSeparatedList(indenter, initializerSyntax.Expressions, token),
            _ => null,
        };

    private static IndentationResult? TryGetIndentationFromCommaSeparatedList<T>(
        Indenter indenter, SeparatedSyntaxList<T> list, SyntaxToken token) where T : SyntaxNode
    {
        var index = list.GetWithSeparators().IndexOf(token);
        if (index < 0)
        {
            return null;
        }

        // find node that starts at the beginning of a line
        var sourceText = indenter.LineToBeIndented.Text;
        RoslynDebug.AssertNotNull(sourceText);
        for (var i = (index - 1) / 2; i >= 0; i--)
        {
            var node = list[i];
            var firstToken = node.GetFirstToken(includeZeroWidth: true);

            if (firstToken.IsFirstTokenOnLine(sourceText))
            {
                return indenter.GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(firstToken.SpanStart));
            }
        }

        return null;
    }

    private static IndentationResult? TryGetIndentationForQueryExpression(Indenter indenter, SyntaxToken token)
    {
        if (!IsPartOfQueryExpression(token))
        {
            return null;
        }

        // find containing non terminal node
        var queryExpressionClause = GetQueryExpressionClause(token);
        if (queryExpressionClause == null)
        {
            return null;
        }

        // find line where first token of the node is
        var sourceText = indenter.LineToBeIndented.Text;
        RoslynDebug.AssertNotNull(sourceText);
        var firstToken = queryExpressionClause.GetFirstToken(includeZeroWidth: true);
        var firstTokenLine = sourceText.Lines.GetLineFromPosition(firstToken.SpanStart);

        // find line where given token is
        var givenTokenLine = sourceText.Lines.GetLineFromPosition(token.SpanStart);

        if (firstTokenLine.LineNumber != givenTokenLine.LineNumber)
        {
            // do default behavior
            return null;
        }

        // okay, we are right under the query expression.
        // align caret to query expression
        if (firstToken.IsFirstTokenOnLine(sourceText))
        {
            return indenter.GetIndentationOfToken(firstToken);
        }

        // find query body that has a token that is a first token on the line
        if (queryExpressionClause.Parent is not QueryBodySyntax queryBody)
        {
            return indenter.GetIndentationOfToken(firstToken);
        }

        // find preceding clause that starts on its own.
        var clauses = queryBody.Clauses;
        for (var i = clauses.Count - 1; i >= 0; i--)
        {
            var clause = clauses[i];
            if (firstToken.SpanStart <= clause.SpanStart)
            {
                continue;
            }

            var clauseToken = clause.GetFirstToken(includeZeroWidth: true);
            if (clauseToken.IsFirstTokenOnLine(sourceText))
            {
                return indenter.GetIndentationOfToken(clauseToken);
            }
        }

        // no query clause start a line. use the first token of the query expression
        RoslynDebug.AssertNotNull(queryBody.Parent);
        return indenter.GetIndentationOfToken(queryBody.Parent.GetFirstToken(includeZeroWidth: true));
    }

    private static SyntaxNode? GetQueryExpressionClause(SyntaxToken token)
    {
        var clause = token.GetAncestors<SyntaxNode>().FirstOrDefault(n => n is QueryClauseSyntax or SelectOrGroupClauseSyntax);

        if (clause != null)
        {
            return clause;
        }

        // If this is a query continuation, use the last clause of its parenting query.
        var body = token.GetAncestor<QueryBodySyntax>();
        if (body != null)
        {
            if (body.SelectOrGroup.IsMissing)
            {
                return body.Clauses.LastOrDefault();
            }
            else
            {
                return body.SelectOrGroup;
            }
        }

        return null;
    }

    private static bool IsPartOfQueryExpression(SyntaxToken token)
    {
        var queryExpression = token.GetAncestor<QueryExpressionSyntax>();
        return queryExpression != null;
    }

    private static IndentationResult? TryGetIndentationFromMultilineStatement(Indenter indenter, SyntaxToken token)
    {
        var nonExpressionNode = token.GetAncestors<SyntaxNode>().FirstOrDefault(n => n is StatementSyntax);
        if (nonExpressionNode != null)
        {
            var sourceText = indenter.LineToBeIndented.Text;
            RoslynDebug.AssertNotNull(sourceText);

            // find line where first token of the node is
            var firstTokenLine = sourceText.Lines.GetLineFromPosition(nonExpressionNode.GetFirstToken(includeZeroWidth: true).SpanStart);

            // find line where given token is
            var givenTokenLine = sourceText.Lines.GetLineFromPosition(token.SpanStart);

            // multiline expression
            if (firstTokenLine.LineNumber != givenTokenLine.LineNumber)
            {
                // okay, looks like containing node is written over multiple lines, in that case, give same indentation as given token
                return indenter.GetIndentationOfLine(givenTokenLine);
            }
        }

        return null;
    }
}
