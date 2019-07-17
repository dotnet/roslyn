// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Indentation
{
    internal partial class CSharpIndentationService
    {
        protected override bool ShouldUseTokenIndenter(Indenter indenter, out SyntaxToken syntaxToken)
            => ShouldUseSmartTokenFormatterInsteadOfIndenter(
                indenter.Rules, indenter.Root, indenter.LineToBeIndented, indenter.OptionSet, out syntaxToken);

        protected override ISmartTokenFormatter CreateSmartTokenFormatter(Indenter indenter)
        {
            var workspace = indenter.Document.Project.Solution.Workspace;
            var formattingRuleFactory = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
            var rules = formattingRuleFactory.CreateRule(indenter.Document.Document, indenter.LineToBeIndented.Start).Concat(Formatter.GetDefaultFormattingRules(indenter.Document.Document));

            return new CSharpSmartTokenFormatter(indenter.OptionSet, rules, indenter.Root);
        }

        protected override IndentationResult GetDesiredIndentationWorker(
            Indenter indenter, SyntaxToken token, TextLine previousLine, int lastNonWhitespacePosition)
        {
            // okay, now check whether the text we found is trivia or actual token.
            if (token.Span.Contains(lastNonWhitespacePosition))
            {
                // okay, it is a token case, do special work based on type of last token on previous line
                return GetIndentationBasedOnToken(indenter, token);
            }
            else
            {
                // there must be trivia that contains or touch this position
                Debug.Assert(token.FullSpan.Contains(lastNonWhitespacePosition));

                // okay, now check whether the trivia is at the beginning of the line
                var firstNonWhitespacePosition = previousLine.GetFirstNonWhitespacePosition();
                if (!firstNonWhitespacePosition.HasValue)
                {
                    return indenter.IndentFromStartOfLine(0);
                }

                var trivia = indenter.Root.FindTrivia(firstNonWhitespacePosition.Value, findInsideTrivia: true);
                if (trivia.Kind() == SyntaxKind.None || indenter.LineToBeIndented.LineNumber > previousLine.LineNumber + 1)
                {
                    // If the token belongs to the next statement and is also the first token of the statement, then it means the user wants
                    // to start type a new statement. So get indentation from the start of the line but not based on the token.
                    // Case:
                    // static void Main(string[] args)
                    // {
                    //     // A
                    //     // B
                    //     
                    //     $$
                    //     return;
                    // }

                    var containingStatement = token.GetAncestor<StatementSyntax>();
                    if (containingStatement != null && containingStatement.GetFirstToken() == token)
                    {
                        var position = indenter.GetCurrentPositionNotBelongToEndOfFileToken(indenter.LineToBeIndented.Start);
                        return indenter.IndentFromStartOfLine(indenter.Finder.GetIndentationOfCurrentPosition(indenter.Tree, token, position, indenter.CancellationToken));
                    }

                    // If the token previous of the base token happens to be a Comma from a separation list then we need to handle it different
                    // Case:
                    // var s = new List<string>
                    //                 {
                    //                     """",
                    //                             """",/*sdfsdfsdfsdf*/
                    //                                  // dfsdfsdfsdfsdf
                    //                                  
                    //                             $$
                    //                 };
                    var previousToken = token.GetPreviousToken();
                    if (previousToken.IsKind(SyntaxKind.CommaToken))
                    {
                        return GetIndentationFromCommaSeparatedList(indenter, previousToken);
                    }
                    else if (!previousToken.IsKind(SyntaxKind.None))
                    {
                        // okay, beginning of the line is not trivia, use the last token on the line as base token
                        return GetIndentationBasedOnToken(indenter, token);
                    }
                }

                // this case we will keep the indentation of this trivia line
                // this trivia can't be preprocessor by the way.
                return indenter.GetIndentationOfLine(previousLine);
            }
        }

        private IndentationResult GetIndentationBasedOnToken(Indenter indenter, SyntaxToken token)
        {
            Contract.ThrowIfNull(indenter.Tree);
            Contract.ThrowIfTrue(token.Kind() == SyntaxKind.None);

            // special cases
            // case 1: token belongs to verbatim token literal
            // case 2: $@"$${0}"
            // case 3: $@"Comment$$ inbetween{0}"
            // case 4: $@"{0}$$"
            if (token.IsVerbatimStringLiteral() ||
                token.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken) ||
                token.IsKind(SyntaxKind.InterpolatedStringTextToken) ||
                (token.IsKind(SyntaxKind.CloseBraceToken) && token.Parent.IsKind(SyntaxKind.Interpolation)))
            {
                return indenter.IndentFromStartOfLine(0);
            }

            // if previous statement belong to labeled statement, don't follow label's indentation
            // but its previous one.
            if (token.Parent is LabeledStatementSyntax || token.IsLastTokenInLabelStatement())
            {
                token = token.GetAncestor<LabeledStatementSyntax>().GetFirstToken(includeZeroWidth: true).GetPreviousToken(includeZeroWidth: true);
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
            var sourceText = indenter.LineToBeIndented.Text;

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
                Debug.Assert(
                    token.Parent != null &&
                    (token.Parent.Parent is StatementSyntax || token.Parent.Parent is ElseClauseSyntax));

                var embeddedStatementOwner = token.Parent.Parent;
                while (embeddedStatementOwner.IsEmbeddedStatement())
                {
                    embeddedStatementOwner = embeddedStatementOwner.Parent;
                }

                return indenter.GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(embeddedStatementOwner.GetFirstToken(includeZeroWidth: true).SpanStart));
            }

            switch (token.Kind())
            {
                case SyntaxKind.SemicolonToken:
                    {
                        // special cases
                        if (token.IsSemicolonInForStatement())
                        {
                            return GetDefaultIndentationFromToken(indenter, token);
                        }

                        return indenter.IndentFromStartOfLine(indenter.Finder.GetIndentationOfCurrentPosition(indenter.Tree, token, position, indenter.CancellationToken));
                    }

                case SyntaxKind.CloseBraceToken:
                    {
                        if (token.Parent.IsKind(SyntaxKind.AccessorList) &&
                            token.Parent.Parent.IsKind(SyntaxKind.PropertyDeclaration))
                        {
                            if (token.GetNextToken().IsEqualsTokenInAutoPropertyInitializers())
                            {
                                return GetDefaultIndentationFromToken(indenter, token);
                            }
                        }

                        return indenter.IndentFromStartOfLine(indenter.Finder.GetIndentationOfCurrentPosition(indenter.Tree, token, position, indenter.CancellationToken));
                    }

                case SyntaxKind.OpenBraceToken:
                    {
                        return indenter.IndentFromStartOfLine(indenter.Finder.GetIndentationOfCurrentPosition(indenter.Tree, token, position, indenter.CancellationToken));
                    }

                case SyntaxKind.ColonToken:
                    {
                        var nonTerminalNode = token.Parent;
                        Contract.ThrowIfNull(nonTerminalNode, @"Malformed code or bug in parser???");

                        if (nonTerminalNode is SwitchLabelSyntax)
                        {
                            return indenter.GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(nonTerminalNode.GetFirstToken(includeZeroWidth: true).SpanStart), indenter.OptionSet.GetOption(FormattingOptions.IndentationSize, token.Language));
                        }

                        goto default;
                    }

                case SyntaxKind.CloseBracketToken:
                    {
                        var nonTerminalNode = token.Parent;
                        Contract.ThrowIfNull(nonTerminalNode, @"Malformed code or bug in parser???");

                        // if this is closing an attribute, we shouldn't indent.
                        if (nonTerminalNode is AttributeListSyntax)
                        {
                            return indenter.GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(nonTerminalNode.GetFirstToken(includeZeroWidth: true).SpanStart));
                        }

                        goto default;
                    }

                case SyntaxKind.XmlTextLiteralToken:
                    {
                        return indenter.GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(token.SpanStart));
                    }

                case SyntaxKind.CommaToken:
                    {
                        return GetIndentationFromCommaSeparatedList(indenter, token);
                    }

                case SyntaxKind.CloseParenToken:
                    {
                        if (token.Parent.IsKind(SyntaxKind.ArgumentList))
                        {
                            return GetDefaultIndentationFromToken(indenter, token.Parent.GetFirstToken(includeZeroWidth: true));
                        }

                        goto default;
                    }

                default:
                    {
                        return GetDefaultIndentationFromToken(indenter, token);
                    }
            }
        }

        private IndentationResult GetIndentationFromCommaSeparatedList(Indenter indenter, SyntaxToken token)
            => token.Parent switch
            {
                BaseArgumentListSyntax argument => GetIndentationFromCommaSeparatedList(indenter, argument.Arguments, token),
                BaseParameterListSyntax parameter => GetIndentationFromCommaSeparatedList(indenter, parameter.Parameters, token),
                TypeArgumentListSyntax typeArgument => GetIndentationFromCommaSeparatedList(indenter, typeArgument.Arguments, token),
                TypeParameterListSyntax typeParameter => GetIndentationFromCommaSeparatedList(indenter, typeParameter.Parameters, token),
                EnumDeclarationSyntax enumDeclaration => GetIndentationFromCommaSeparatedList(indenter, enumDeclaration.Members, token),
                InitializerExpressionSyntax initializerSyntax => GetIndentationFromCommaSeparatedList(indenter, initializerSyntax.Expressions, token),
                _ => GetDefaultIndentationFromToken(indenter, token),
            };

        private IndentationResult GetIndentationFromCommaSeparatedList<T>(
            Indenter indenter, SeparatedSyntaxList<T> list, SyntaxToken token) where T : SyntaxNode
        {
            var index = list.GetWithSeparators().IndexOf(token);
            if (index < 0)
            {
                return GetDefaultIndentationFromToken(indenter, token);
            }

            // find node that starts at the beginning of a line
            var sourceText = indenter.LineToBeIndented.Text;
            for (var i = (index - 1) / 2; i >= 0; i--)
            {
                var node = list[i];
                var firstToken = node.GetFirstToken(includeZeroWidth: true);

                if (firstToken.IsFirstTokenOnLine(sourceText))
                {
                    return indenter.GetIndentationOfLine(sourceText.Lines.GetLineFromPosition(firstToken.SpanStart));
                }
            }

            // smart indenter has a special indent block rule for comma separated list, so don't
            // need to add default additional space for multiline expressions
            return GetDefaultIndentationFromTokenLine(indenter, token, additionalSpace: 0);
        }

        private IndentationResult GetDefaultIndentationFromToken(Indenter indenter, SyntaxToken token)
        {
            if (IsPartOfQueryExpression(token))
            {
                return GetIndentationForQueryExpression(indenter, token);
            }

            return GetDefaultIndentationFromTokenLine(indenter, token);
        }

        private IndentationResult GetIndentationForQueryExpression(Indenter indenter, SyntaxToken token)
        {
            // find containing non terminal node
            var queryExpressionClause = GetQueryExpressionClause(token);
            if (queryExpressionClause == null)
            {
                return GetDefaultIndentationFromTokenLine(indenter, token);
            }

            // find line where first token of the node is
            var sourceText = indenter.LineToBeIndented.Text;
            var firstToken = queryExpressionClause.GetFirstToken(includeZeroWidth: true);
            var firstTokenLine = sourceText.Lines.GetLineFromPosition(firstToken.SpanStart);

            // find line where given token is
            var givenTokenLine = sourceText.Lines.GetLineFromPosition(token.SpanStart);

            if (firstTokenLine.LineNumber != givenTokenLine.LineNumber)
            {
                // do default behavior
                return GetDefaultIndentationFromTokenLine(indenter, token);
            }

            // okay, we are right under the query expression.
            // align caret to query expression
            if (firstToken.IsFirstTokenOnLine(sourceText))
            {
                return indenter.GetIndentationOfToken(firstToken);
            }

            // find query body that has a token that is a first token on the line
            if (!(queryExpressionClause.Parent is QueryBodySyntax queryBody))
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
            return indenter.GetIndentationOfToken(queryBody.Parent.GetFirstToken(includeZeroWidth: true));
        }

        private SyntaxNode GetQueryExpressionClause(SyntaxToken token)
        {
            var clause = token.GetAncestors<SyntaxNode>().FirstOrDefault(n => n is QueryClauseSyntax || n is SelectOrGroupClauseSyntax);

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

        private bool IsPartOfQueryExpression(SyntaxToken token)
        {
            var queryExpression = token.GetAncestor<QueryExpressionSyntax>();
            return queryExpression != null;
        }

        private IndentationResult GetDefaultIndentationFromTokenLine(
            Indenter indenter, SyntaxToken token, int? additionalSpace = null)
        {
            var spaceToAdd = additionalSpace ?? indenter.OptionSet.GetOption(FormattingOptions.IndentationSize, token.Language);

            var sourceText = indenter.LineToBeIndented.Text;

            // find line where given token is
            var givenTokenLine = sourceText.Lines.GetLineFromPosition(token.SpanStart);

            // find right position
            var position = indenter.GetCurrentPositionNotBelongToEndOfFileToken(indenter.LineToBeIndented.Start);

            // find containing non expression node
            var nonExpressionNode = token.GetAncestors<SyntaxNode>().FirstOrDefault(n => n is StatementSyntax);
            if (nonExpressionNode == null)
            {
                // well, I can't find any non expression node. use default behavior
                return indenter.IndentFromStartOfLine(indenter.Finder.GetIndentationOfCurrentPosition(indenter.Tree, token, position, spaceToAdd, indenter.CancellationToken));
            }

            // find line where first token of the node is
            var firstTokenLine = sourceText.Lines.GetLineFromPosition(nonExpressionNode.GetFirstToken(includeZeroWidth: true).SpanStart);

            // single line expression
            if (firstTokenLine.LineNumber == givenTokenLine.LineNumber)
            {
                return indenter.IndentFromStartOfLine(indenter.Finder.GetIndentationOfCurrentPosition(indenter.Tree, token, position, spaceToAdd, indenter.CancellationToken));
            }

            // okay, looks like containing node is written over multiple lines, in that case, give same indentation as given token
            return indenter.GetIndentationOfLine(givenTokenLine);
        }
    }
}
