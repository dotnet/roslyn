// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    internal partial class Worker
    {
        private void ClassifyPreprocessorDirective(DirectiveTriviaSyntax node)
        {
            if (!_textSpan.OverlapsWith(node.Span))
            {
                return;
            }

            switch (node.Kind())
            {
                case SyntaxKind.IfDirectiveTrivia:
                    ClassifyIfDirective((IfDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.ElifDirectiveTrivia:
                    ClassifyElifDirective((ElifDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.ElseDirectiveTrivia:
                    ClassifyElseDirective((ElseDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.EndIfDirectiveTrivia:
                    ClassifyEndIfDirective((EndIfDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.RegionDirectiveTrivia:
                    ClassifyRegionDirective((RegionDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.EndRegionDirectiveTrivia:
                    ClassifyEndRegionDirective((EndRegionDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.ErrorDirectiveTrivia:
                    ClassifyErrorDirective((ErrorDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.WarningDirectiveTrivia:
                    ClassifyWarningDirective((WarningDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.BadDirectiveTrivia:
                    ClassifyBadDirective((BadDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.DefineDirectiveTrivia:
                    ClassifyDefineDirective((DefineDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.UndefDirectiveTrivia:
                    ClassifyUndefDirective((UndefDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.LineDirectiveTrivia:
                    ClassifyLineDirective((LineDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.PragmaChecksumDirectiveTrivia:
                    ClassifyPragmaChecksumDirective((PragmaChecksumDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.PragmaWarningDirectiveTrivia:
                    ClassifyPragmaWarningDirective((PragmaWarningDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.ReferenceDirectiveTrivia:
                    ClassifyReferenceDirective((ReferenceDirectiveTriviaSyntax)node);
                    break;
                case SyntaxKind.LoadDirectiveTrivia:
                    ClassifyLoadDirective((LoadDirectiveTriviaSyntax)node);
                    break;
            }
        }

        private void ClassifyDirectiveTrivia(DirectiveTriviaSyntax node, bool allowComments = true)
        {
            var lastToken = node.EndOfDirectiveToken.GetPreviousToken(includeSkipped: false);

            foreach (var trivia in lastToken.TrailingTrivia)
            {
                // skip initial whitespace
                if (trivia.Kind() == SyntaxKind.WhitespaceTrivia)
                {
                    continue;
                }

                ClassifyPreprocessorTrivia(trivia, allowComments);
            }

            foreach (var trivia in node.EndOfDirectiveToken.LeadingTrivia)
            {
                ClassifyPreprocessorTrivia(trivia, allowComments);
            }
        }

        private void ClassifyPreprocessorTrivia(SyntaxTrivia trivia, bool allowComments)
        {
            if (allowComments && trivia.Kind() == SyntaxKind.SingleLineCommentTrivia)
            {
                AddClassification(trivia, ClassificationTypeKind.Comment);
            }
            else
            {
                AddClassification(trivia, ClassificationTypeKind.PreprocessorText);
            }
        }

        private void ClassifyPreprocessorExpression(ExpressionSyntax node)
        {
            if (node == null)
            {
                return;
            }

            if (node is LiteralExpressionSyntax literal)
            {
                // true or false
                AddClassification(literal.Token, ClassificationTypeKind.Keyword);
            }
            else if (node is IdentifierNameSyntax identifier)
            {
                // DEBUG
                AddClassification(identifier.Identifier, ClassificationTypeKind.Identifier);
            }
            else if (node is ParenthesizedExpressionSyntax parenExpression)
            {
                // (true)
                AddClassification(parenExpression.OpenParenToken, ClassificationTypeKind.Punctuation);
                ClassifyPreprocessorExpression(parenExpression.Expression);
                AddClassification(parenExpression.CloseParenToken, ClassificationTypeKind.Punctuation);
            }
            else if (node is PrefixUnaryExpressionSyntax prefixExpression)
            {
                // !
                AddClassification(prefixExpression.OperatorToken, ClassificationTypeKind.Operator);
                ClassifyPreprocessorExpression(prefixExpression.Operand);
            }
            else if (node is BinaryExpressionSyntax binaryExpression)
            {
                // &&, ||, ==, !=
                ClassifyPreprocessorExpression(binaryExpression.Left);
                AddClassification(binaryExpression.OperatorToken, ClassificationTypeKind.Operator);
                ClassifyPreprocessorExpression(binaryExpression.Right);
            }
        }

        private void ClassifyIfDirective(IfDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.IfKeyword, ClassificationTypeKind.PreprocessorKeyword);
            ClassifyPreprocessorExpression(node.Condition);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyElifDirective(ElifDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.ElifKeyword, ClassificationTypeKind.PreprocessorKeyword);
            ClassifyPreprocessorExpression(node.Condition);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyElseDirective(ElseDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.ElseKeyword, ClassificationTypeKind.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyEndIfDirective(EndIfDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.EndIfKeyword, ClassificationTypeKind.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyErrorDirective(ErrorDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.ErrorKeyword, ClassificationTypeKind.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node, allowComments: false);
        }

        private void ClassifyWarningDirective(WarningDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.WarningKeyword, ClassificationTypeKind.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node, allowComments: false);
        }

        private void ClassifyRegionDirective(RegionDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.RegionKeyword, ClassificationTypeKind.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node, allowComments: false);
        }

        private void ClassifyEndRegionDirective(EndRegionDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.EndRegionKeyword, ClassificationTypeKind.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyDefineDirective(DefineDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.DefineKeyword, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.Name, ClassificationTypeKind.Identifier);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyUndefDirective(UndefDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.UndefKeyword, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.Name, ClassificationTypeKind.Identifier);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyBadDirective(BadDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.Identifier, ClassificationTypeKind.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyLineDirective(LineDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.LineKeyword, ClassificationTypeKind.PreprocessorKeyword);

            switch (node.Line.Kind())
            {
                case SyntaxKind.HiddenKeyword:
                case SyntaxKind.DefaultKeyword:
                    AddClassification(node.Line, ClassificationTypeKind.PreprocessorKeyword);
                    break;
                case SyntaxKind.NumericLiteralToken:
                    AddClassification(node.Line, ClassificationTypeKind.NumericLiteral);
                    break;
            }

            if (node.File.Kind() != SyntaxKind.None)
            {
                AddClassification(node.File, ClassificationTypeKind.StringLiteral);
            }

            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyPragmaChecksumDirective(PragmaChecksumDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.PragmaKeyword, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.ChecksumKeyword, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.File, ClassificationTypeKind.StringLiteral);
            AddClassification(node.Guid, ClassificationTypeKind.StringLiteral);
            AddClassification(node.Bytes, ClassificationTypeKind.StringLiteral);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyPragmaWarningDirective(PragmaWarningDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.PragmaKeyword, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.WarningKeyword, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.DisableOrRestoreKeyword, ClassificationTypeKind.PreprocessorKeyword);

            foreach (var nodeOrToken in node.ErrorCodes.GetWithSeparators())
            {
                ClassifyNodeOrToken(nodeOrToken);
            }
        }

        private void ClassifyReferenceDirective(ReferenceDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.ReferenceKeyword, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.File, ClassificationTypeKind.StringLiteral);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyLoadDirective(LoadDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.LoadKeyword, ClassificationTypeKind.PreprocessorKeyword);
            AddClassification(node.File, ClassificationTypeKind.StringLiteral);
            ClassifyDirectiveTrivia(node);
        }
    }
}
