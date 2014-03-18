// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class SyntaxFormatter : CSharpSyntaxRewriter
    {
        private readonly string indentWhitespace;
        private readonly bool useElasticTrivia;

        private bool isInStructuredTrivia;

        private SyntaxToken previousToken;
        private bool indentNext;

        private bool afterLineBreak;
        private bool afterIndentation;

        // CONSIDER: if we become concerned about space, we shouldn't actually need any 
        // of the values between indentations[0] and indentations[initialDepth] (exclusive).
        private ArrayBuilder<SyntaxTrivia> indentations;

        private SyntaxFormatter(string indentWhitespace, bool useElasticTrivia)
            : base(visitIntoStructuredTrivia: true)
        {
            this.indentWhitespace = indentWhitespace;
            this.useElasticTrivia = useElasticTrivia;

            this.afterLineBreak = true;
        }

        internal static TNode Format<TNode>(TNode node, string indentWhitespace, bool useElasticTrivia = false)
            where TNode : SyntaxNode
        {
            var formatter = new SyntaxFormatter(indentWhitespace, useElasticTrivia);
            var result = (TNode)formatter.Visit(node);
            formatter.Free();
            return result;
        }

        internal static SyntaxToken Format(SyntaxToken token, string indentWhitespace, bool useElasticTrivia = false)
        {
            var formatter = new SyntaxFormatter(indentWhitespace, useElasticTrivia);
            var result = formatter.VisitToken(token);
            formatter.Free();
            return result;
        }

        internal static SyntaxTriviaList Format(SyntaxTriviaList trivia, string indentWhitespace, bool useElasticTrivia = false)
        {
            var formatter = new SyntaxFormatter(indentWhitespace, useElasticTrivia);
            var result = formatter.RewriteTrivia(
                trivia,
                GetDeclarationDepth((SyntaxToken)trivia.ElementAt(0).Token),
                isTrailing: false,
                mustBeIndented: false,
                mustHaveSeparator: false,
                lineBreaksAfter: 0);
            formatter.Free();
            return result;
        }

        private void Free()
        {
            if (indentations != null)
            {
                indentations.Free();
            }
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (token.CSharpKind() == SyntaxKind.None || (token.IsMissing && token.FullWidth == 0))
            {
                return token;
            }

            try
            {
                var tk = token;

                var depth = GetDeclarationDepth(token);
                var needsIndentation = this.indentNext || (LineBreaksAfter(previousToken, token) > 0);
                var lineBreaksAfter = LineBreaksAfter(token);
                this.indentNext = false;

                tk = tk.WithLeadingTrivia(RewriteTrivia(
                    token.LeadingTrivia,
                    depth,
                    isTrailing: false,
                    mustBeIndented: needsIndentation,
                    mustHaveSeparator: false,
                    lineBreaksAfter: lineBreaksAfter));

                // get next token, skipping zero width tokens except for end-of-directive tokens
                var nextToken = token.GetNextToken(t => SyntaxToken.NonZeroWidth(t) || t.CSharpKind() == SyntaxKind.EndOfDirectiveToken, t => t.CSharpKind() == SyntaxKind.SkippedTokensTrivia);

                this.afterLineBreak = EndsInLineBreak(token);
                this.afterIndentation = false;

                lineBreaksAfter = LineBreaksAfter(token, nextToken);
                var needsSeparatorAfter = NeedsSeparator(token, nextToken);
                tk = tk.WithTrailingTrivia(RewriteTrivia(
                    token.TrailingTrivia,
                    depth,
                    isTrailing: true,
                    mustBeIndented: false,
                    mustHaveSeparator: needsSeparatorAfter,
                    lineBreaksAfter: lineBreaksAfter));

                if (lineBreaksAfter > 0)
                {
                    indentNext = true;
                }

                return tk;
            }
            finally
            {
                this.previousToken = token;
            }
        }

        private SyntaxTrivia GetIndentation(int count)
        {
            int capacity = count + 1;
            if (indentations == null)
            {
                indentations = ArrayBuilder<SyntaxTrivia>.GetInstance(capacity);
            }
            else
            {
                indentations.EnsureCapacity(capacity);
            }

            for (int i = indentations.Count; i <= count; i++)
            {
                string text = i == 0
                    ? ""
                    : indentations[i - 1].ToString() + this.indentWhitespace;
                indentations.Add(SyntaxFactory.Whitespace(text, this.useElasticTrivia));
            }

            return indentations[count];
        }

        private static int LineBreaksAfter(SyntaxToken token)
        {
            return token.CSharpKind() == SyntaxKind.EndOfDirectiveToken ? 1 : 0;
        }

        private int LineBreaksAfter(SyntaxToken currentToken, SyntaxToken nextToken)
        {
            if (nextToken.CSharpKind() == SyntaxKind.None)
            {
                return 0;
            }

            // none of the following tests currently have meaning for structured trivia
            if (this.isInStructuredTrivia)
            {
                return 0;
            }

            switch (currentToken.CSharpKind())
            {
                case SyntaxKind.None:
                    return 0;
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.FinallyKeyword:
                    return 1;
                case SyntaxKind.CloseBraceToken:
                    return LineBreaksAfterCloseBrace(nextToken);

                case SyntaxKind.CloseParenToken:
                    return (((currentToken.Parent is StatementSyntax) && nextToken.Parent != currentToken.Parent)
                        || nextToken.CSharpKind() == SyntaxKind.OpenBraceToken) ? 1 : 0;
                case SyntaxKind.CloseBracketToken:
                    if (currentToken.Parent is AttributeListSyntax)
                    {
                        return 1;
                    }

                    break;
                case SyntaxKind.SemicolonToken:
                    return LineBreaksAfterSemicolon(currentToken, nextToken);

                case SyntaxKind.CommaToken:
                    return currentToken.Parent is EnumDeclarationSyntax ? 1 : 0;
                case SyntaxKind.ElseKeyword:
                    return nextToken.CSharpKind() != SyntaxKind.IfKeyword ? 1 : 0;
                case SyntaxKind.ColonToken:
                    if (currentToken.Parent is LabeledStatementSyntax || currentToken.Parent is SwitchLabelSyntax)
                    {
                        return 1;
                    }
                    break;
            }

            if ((nextToken.IsKind(SyntaxKind.FromKeyword) && nextToken.Parent.IsKind(SyntaxKind.FromClause)) ||
                (nextToken.IsKind(SyntaxKind.LetKeyword) && nextToken.Parent.IsKind(SyntaxKind.LetClause)) ||
                (nextToken.IsKind(SyntaxKind.WhereKeyword) && nextToken.Parent.IsKind(SyntaxKind.WhereClause)) ||
                (nextToken.IsKind(SyntaxKind.JoinKeyword) && nextToken.Parent.IsKind(SyntaxKind.JoinClause)) ||
                (nextToken.IsKind(SyntaxKind.JoinKeyword) && nextToken.Parent.CSharpKind() == SyntaxKind.JoinIntoClause) ||
                (nextToken.CSharpKind() == SyntaxKind.OrderByKeyword && nextToken.Parent.CSharpKind() == SyntaxKind.OrderByClause) ||
                (nextToken.CSharpKind() == SyntaxKind.SelectKeyword && nextToken.Parent.CSharpKind() == SyntaxKind.SelectClause) ||
                (nextToken.CSharpKind() == SyntaxKind.GroupKeyword && nextToken.Parent.CSharpKind() == SyntaxKind.GroupClause))
            {
                return 1;
            }

            switch (nextToken.CSharpKind())
            {
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.CloseBraceToken:
                case SyntaxKind.ElseKeyword:
                case SyntaxKind.FinallyKeyword:
                    return 1;
                case SyntaxKind.OpenBracketToken:
                    return nextToken.Parent is AttributeListSyntax ? 1 : 0;
                case SyntaxKind.WhereKeyword:
                    return currentToken.Parent is TypeParameterListSyntax ? 1 : 0;
            }

            return 0;
        }

        private static int LineBreaksAfterCloseBrace(SyntaxToken nextToken)
        {
            if (nextToken.CSharpKind() == SyntaxKind.CloseBraceToken)
            {
                return 1;
            }
            else if (
                nextToken.CSharpKind() == SyntaxKind.CatchKeyword ||
                nextToken.CSharpKind() == SyntaxKind.FinallyKeyword ||
                nextToken.CSharpKind() == SyntaxKind.ElseKeyword)
            {
                return 1;
            }
            else if (
                nextToken.CSharpKind() == SyntaxKind.WhileKeyword &&
                nextToken.Parent.CSharpKind() == SyntaxKind.DoStatement)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        private static int LineBreaksAfterSemicolon(SyntaxToken currentToken, SyntaxToken nextToken)
        {
            if (currentToken.Parent.CSharpKind() == SyntaxKind.ForStatement)
            {
                return 0;
            }
            else if (nextToken.CSharpKind() == SyntaxKind.CloseBraceToken)
            {
                return 1;
            }
            else if (currentToken.Parent.CSharpKind() == SyntaxKind.UsingDirective)
            {
                return nextToken.Parent.CSharpKind() == SyntaxKind.UsingDirective ? 1 : 2;
            }
            else if (currentToken.Parent.CSharpKind() == SyntaxKind.ExternAliasDirective)
            {
                return nextToken.Parent.CSharpKind() == SyntaxKind.ExternAliasDirective ? 1 : 2;
            }
            else
            {
                return 1;
            }
        }

        private bool NeedsSeparator(SyntaxToken token, SyntaxToken next)
        {
            if (token.Parent == null || next.Parent == null)
            {
                return false;
            }

            if (IsXmlTextToken(token.CSharpKind()) || IsXmlTextToken(token.CSharpKind()))
            {
                return false;
            }

            if (next.CSharpKind() == SyntaxKind.EndOfDirectiveToken)
            {
                // In a region directive, there's no token between the region keyword and 
                // the end-of-directive, so we may need a separator.
                return token.CSharpKind() == SyntaxKind.RegionKeyword && next.LeadingWidth > 0;
            }

            if ((token.Parent is BinaryExpressionSyntax && BinaryTokenNeedsSeparator(token.CSharpKind())) ||
                (next.Parent is BinaryExpressionSyntax && BinaryTokenNeedsSeparator(next.CSharpKind())))
            {
                return true;
            }

            if (token.IsKind(SyntaxKind.GreaterThanToken) && token.Parent.IsKind(SyntaxKind.TypeArgumentList))
            {
                if (!SyntaxFacts.IsPunctuation(next.CSharpKind()))
                {
                    return true;
                }
            }

            if (token.IsKind(SyntaxKind.CommaToken) &&
                !next.IsKind(SyntaxKind.CommaToken) &&
                !token.Parent.IsKind(SyntaxKind.EnumDeclaration))
            {
                return true;
            }

            if (token.CSharpKind() == SyntaxKind.SemicolonToken
                && !(next.CSharpKind() == SyntaxKind.SemicolonToken || next.CSharpKind() == SyntaxKind.CloseParenToken))
            {
                return true;
            }

            if (token.IsKind(SyntaxKind.QuestionToken)
                && (token.Parent.IsKind(SyntaxKind.ConditionalExpression) || token.Parent is TypeSyntax))
            {
                return true;
            }

            if (token.IsKind(SyntaxKind.ColonToken))
            {
                return true;
            }

            if (next.IsKind(SyntaxKind.ColonToken))
            {
                if (next.Parent.IsKind(SyntaxKind.BaseList) ||
                    next.Parent.IsKind(SyntaxKind.TypeParameterConstraintClause))
                {
                    return true;
                }
            }

            if (token.IsKind(SyntaxKind.CloseBracketToken) && IsWord(next.CSharpKind()))
            {
                return true;
            }

            if ((next.IsKind(SyntaxKind.QuestionToken) || next.IsKind(SyntaxKind.ColonToken))
                && (next.Parent.IsKind(SyntaxKind.ConditionalExpression)))
            {
                return true;
            }

            if (token.IsKind(SyntaxKind.EqualsToken) || next.IsKind(SyntaxKind.EqualsToken))
            {
                return true;
            }

            if (token.IsKind(SyntaxKind.EqualsGreaterThanToken) || next.IsKind(SyntaxKind.EqualsGreaterThanToken))
            {
                return true;
            }

            // Can happen in directives (e.g. #line 1 "file")
            if (SyntaxFacts.IsLiteral(token.CSharpKind()) && SyntaxFacts.IsLiteral(next.CSharpKind()))
            {
                return true;
            }

            if (IsKeyword(token.CSharpKind()))
            {
                if (!next.IsKind(SyntaxKind.ColonToken) &&
                    !next.IsKind(SyntaxKind.DotToken) &&
                    !next.IsKind(SyntaxKind.SemicolonToken) &&
                    !next.IsKind(SyntaxKind.OpenBracketToken) &&
                    !next.IsKind(SyntaxKind.CloseParenToken) &&
                    !next.IsKind(SyntaxKind.CloseBraceToken) &&
                    !next.IsKind(SyntaxKind.ColonColonToken) &&
                    !next.IsKind(SyntaxKind.GreaterThanToken) &&
                    !next.IsKind(SyntaxKind.CommaToken))
                {
                    return true;
                }
            }

            if (IsWord(token.CSharpKind()) && IsWord(next.CSharpKind()))
            {
                return true;
            }
            else if (token.Width > 1 && next.Width > 1)
            {
                var tokenLastChar = token.Text.Last();
                var nextFirstChar = next.Text.First();
                if (tokenLastChar == nextFirstChar && TokenCharacterCanBeDoubled(tokenLastChar))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsXmlTextToken(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.XmlTextLiteralNewLineToken:
                case SyntaxKind.XmlTextLiteralToken:
                    return true;
                default:
                    return false;
            }
        }

        private static bool BinaryTokenNeedsSeparator(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.DotToken:
                case SyntaxKind.MinusGreaterThanToken:
                    return false;
                default:
                    return SyntaxFacts.GetBinaryExpression(kind) != SyntaxKind.None;
            }
        }

        private SyntaxTriviaList RewriteTrivia(
            SyntaxTriviaList triviaList,
            int depth,
            bool isTrailing,
            bool mustBeIndented,
            bool mustHaveSeparator,
            int lineBreaksAfter)
        {
            ArrayBuilder<SyntaxTrivia> currentTriviaList = ArrayBuilder<SyntaxTrivia>.GetInstance(triviaList.Count);
            try
            {
                foreach (var trivia in triviaList)
                {
                    if (trivia.CSharpKind() == SyntaxKind.WhitespaceTrivia ||
                        trivia.CSharpKind() == SyntaxKind.EndOfLineTrivia ||
                        trivia.FullWidth == 0)
                    {
                        continue;
                    }

                    var needsSeparator =
                        (currentTriviaList.Count > 0 && NeedsSeparatorBetween(currentTriviaList.Last())) ||
                            (currentTriviaList.Count == 0 && isTrailing);
                    var needsLineBreak = NeedsLineBreakBefore(trivia) || (currentTriviaList.Count > 0 && NeedsLineBreakBetween(currentTriviaList.Last(), trivia, isTrailing));

                    if (needsLineBreak && !afterLineBreak)
                    {
                        currentTriviaList.Add(GetCarriageReturnLineFeed());
                        afterLineBreak = true;
                        afterIndentation = false;
                    }

                    if (afterLineBreak)
                    {
                        if (!afterIndentation && NeedsIndentAfterLineBreak(trivia))
                        {
                            currentTriviaList.Add(this.GetIndentation(GetDeclarationDepth(trivia)));
                            afterIndentation = true;
                        }
                    }
                    else if (needsSeparator)
                    {
                        currentTriviaList.Add(GetSpace());
                        afterLineBreak = false;
                        afterIndentation = false;
                    }

                    if (trivia.HasStructure)
                    {
                        var tr = this.VisitStructuredTrivia(trivia);
                        currentTriviaList.Add(tr);
                    }
                    else
                    {
                        currentTriviaList.Add(trivia);
                    }

                    if (NeedsLineBreakAfter(trivia, isTrailing))
                    {
                        currentTriviaList.Add(GetCarriageReturnLineFeed());
                        afterLineBreak = true;
                        afterIndentation = false;
                    }
                }

                if (lineBreaksAfter > 0)
                {
                    if (currentTriviaList.Count > 0 && EndsInLineBreak(currentTriviaList.Last()))
                    {
                        lineBreaksAfter--;
                    }

                    for (int i = 0; i < lineBreaksAfter; i++)
                    {
                        currentTriviaList.Add(GetCarriageReturnLineFeed());
                        afterLineBreak = true;
                        afterIndentation = false;
                    }
                }
                else if (mustBeIndented)
                {
                    currentTriviaList.Add(this.GetIndentation(depth));
                    afterIndentation = true;
                }
                else if (mustHaveSeparator)
                {
                    currentTriviaList.Add(GetSpace());
                    afterLineBreak = false;
                    afterIndentation = false;
                }

                if (currentTriviaList.Count == 0)
                {
                    return default(SyntaxTriviaList);
                }
                else if (currentTriviaList.Count == 1)
                {
                    return SyntaxFactory.TriviaList(currentTriviaList.First());
                }
                else
                {
                    return SyntaxFactory.TriviaList(currentTriviaList);
                }
            }
            finally
            {
                currentTriviaList.Free();
            }
        }

        private SyntaxTrivia GetSpace()
        {
            return this.useElasticTrivia ? SyntaxFactory.ElasticSpace : SyntaxFactory.Space;
        }

        private SyntaxTrivia GetCarriageReturnLineFeed()
        {
            return this.useElasticTrivia ? SyntaxFactory.ElasticCarriageReturnLineFeed : SyntaxFactory.CarriageReturnLineFeed;
        }

        private SyntaxTrivia VisitStructuredTrivia(SyntaxTrivia trivia)
        {
            bool oldIsInStructuredTrivia = this.isInStructuredTrivia;
            this.isInStructuredTrivia = true;

            SyntaxToken oldPreviousToken = this.previousToken;
            this.previousToken = default(SyntaxToken);

            SyntaxTrivia result = VisitTrivia(trivia);

            this.isInStructuredTrivia = oldIsInStructuredTrivia;
            this.previousToken = oldPreviousToken;

            return result;
        }

        private static bool NeedsSeparatorBetween(SyntaxTrivia trivia)
        {
            switch (trivia.CSharpKind())
            {
                case SyntaxKind.None:
                case SyntaxKind.WhitespaceTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                    return false;
                default:
                    return !SyntaxFacts.IsPreprocessorDirective(trivia.CSharpKind());
            }
        }

        private static bool NeedsLineBreakBetween(SyntaxTrivia trivia, SyntaxTrivia next, bool isTrailingTrivia)
        {
            if (EndsInLineBreak(trivia))
            {
                return false;
            }

            switch (next.CSharpKind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                    return !isTrailingTrivia;
                default:
                    return false;
            }
        }

        private static bool NeedsLineBreakBefore(SyntaxTrivia trivia)
        {
            switch (trivia.CSharpKind())
            {
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                    return true;
                default:
                    return false;
            }
        }

        private static bool NeedsLineBreakAfter(SyntaxTrivia trivia, bool isTrailingTrivia)
        {
            switch (trivia.CSharpKind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                    return true;
                case SyntaxKind.MultiLineCommentTrivia:
                    return !isTrailingTrivia;
                default:
                    return false;
            }
        }

        private static bool NeedsIndentAfterLineBreak(SyntaxTrivia trivia)
        {
            switch (trivia.CSharpKind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    return true;
                default:
                    return false;
            }
        }

        private static bool EndsInLineBreak(SyntaxToken token)
        {
            return token.CSharpKind() == SyntaxKind.XmlTextLiteralNewLineToken;
        }

        private static bool EndsInLineBreak(SyntaxTrivia trivia)
        {
            if (trivia.CSharpKind() == SyntaxKind.EndOfLineTrivia)
            {
                return true;
            }

            if (trivia.CSharpKind() == SyntaxKind.PreprocessingMessageTrivia || trivia.CSharpKind() == SyntaxKind.DisabledTextTrivia)
            {
                var text = trivia.ToFullString();
                return text.Length > 0 && SyntaxFacts.IsNewLine(text.Last());
            }

            return false;
        }

        private static bool IsWord(SyntaxKind kind)
        {
            return kind == SyntaxKind.IdentifierToken || IsKeyword(kind);
        }

        private static bool IsKeyword(SyntaxKind kind)
        {
            return SyntaxFacts.IsKeywordKind(kind) || SyntaxFacts.IsPreprocessorKeyword(kind);
        }

        private static bool TokenCharacterCanBeDoubled(char c)
        {
            switch (c)
            {
                case '+':
                case '-':
                case '<':
                case ':':
                case '?':
                case '=':
                case '"':
                    return true;
                default:
                    return false;
            }
        }

        private static int GetDeclarationDepth(SyntaxToken token)
        {
            return GetDeclarationDepth(token.Parent);
        }

        private static int GetDeclarationDepth(SyntaxTrivia trivia)
        {
            if (SyntaxFacts.IsPreprocessorDirective(trivia.CSharpKind()))
            {
                return 0;
            }

            return GetDeclarationDepth((SyntaxToken)trivia.Token);
        }

        private static int GetDeclarationDepth(SyntaxNode node)
        {
            if (node != null)
            {
                if (node.IsStructuredTrivia)
                {
                    var tr = ((StructuredTriviaSyntax)node).ParentTrivia;
                    return GetDeclarationDepth(tr);
                }
                else if (node.Parent != null)
                {
                    if (node.Parent.IsKind(SyntaxKind.CompilationUnit))
                    {
                        return 0;
                    }

                    int parentDepth = GetDeclarationDepth(node.Parent);

                    if (node.Parent.IsKind(SyntaxKind.GlobalStatement))
                    {
                        return parentDepth;
                    }

                    if (node.IsKind(SyntaxKind.IfStatement) && node.Parent.IsKind(SyntaxKind.ElseClause))
                    {
                        return parentDepth;
                    }

                    if (node.Parent is BlockSyntax ||
                        (node is StatementSyntax && !(node is BlockSyntax)))
                    {
                        // all nested statements are indented one level
                        return parentDepth + 1;
                    }

                    if (node is MemberDeclarationSyntax ||
                        node is AccessorDeclarationSyntax ||
                        node is TypeParameterConstraintClauseSyntax ||
                        node is SwitchSectionSyntax ||
                        node is UsingDirectiveSyntax ||
                        node is ExternAliasDirectiveSyntax ||
                        node is QueryExpressionSyntax ||
                        node is QueryContinuationSyntax)
                    {
                        return parentDepth + 1;
                    }

                    return parentDepth;
                }
            }

            return 0;
        }
    }
}