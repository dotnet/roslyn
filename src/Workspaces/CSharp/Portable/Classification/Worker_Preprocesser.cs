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
                case SyntaxKind.NullableDirectiveTrivia:
                    ClassifyNullableDirective((NullableDirectiveTriviaSyntax)node);
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
                AddClassification(trivia, ClassificationTypeNames.Comment);
            }
            else
            {
                AddClassification(trivia, ClassificationTypeNames.PreprocessorText);
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
                AddClassification(literal.Token, ClassificationTypeNames.Keyword);
            }
            else if (node is IdentifierNameSyntax identifier)
            {
                // DEBUG
                AddClassification(identifier.Identifier, ClassificationTypeNames.Identifier);
            }
            else if (node is ParenthesizedExpressionSyntax parenExpression)
            {
                // (true)
                AddClassification(parenExpression.OpenParenToken, ClassificationTypeNames.Punctuation);
                ClassifyPreprocessorExpression(parenExpression.Expression);
                AddClassification(parenExpression.CloseParenToken, ClassificationTypeNames.Punctuation);
            }
            else if (node is PrefixUnaryExpressionSyntax prefixExpression)
            {
                // !
                AddClassification(prefixExpression.OperatorToken, ClassificationTypeNames.Operator);
                ClassifyPreprocessorExpression(prefixExpression.Operand);
            }
            else if (node is BinaryExpressionSyntax binaryExpression)
            {
                // &&, ||, ==, !=
                ClassifyPreprocessorExpression(binaryExpression.Left);
                AddClassification(binaryExpression.OperatorToken, ClassificationTypeNames.Operator);
                ClassifyPreprocessorExpression(binaryExpression.Right);
            }
        }

        private void ClassifyIfDirective(IfDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.IfKeyword, ClassificationTypeNames.PreprocessorKeyword);
            ClassifyPreprocessorExpression(node.Condition);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyElifDirective(ElifDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.ElifKeyword, ClassificationTypeNames.PreprocessorKeyword);
            ClassifyPreprocessorExpression(node.Condition);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyElseDirective(ElseDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.ElseKeyword, ClassificationTypeNames.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyEndIfDirective(EndIfDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.EndIfKeyword, ClassificationTypeNames.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyErrorDirective(ErrorDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.ErrorKeyword, ClassificationTypeNames.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node, allowComments: false);
        }

        private void ClassifyWarningDirective(WarningDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.WarningKeyword, ClassificationTypeNames.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node, allowComments: false);
        }

        private void ClassifyRegionDirective(RegionDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.RegionKeyword, ClassificationTypeNames.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node, allowComments: false);
        }

        private void ClassifyEndRegionDirective(EndRegionDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.EndRegionKeyword, ClassificationTypeNames.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyDefineDirective(DefineDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.DefineKeyword, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.Name, ClassificationTypeNames.Identifier);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyUndefDirective(UndefDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.UndefKeyword, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.Name, ClassificationTypeNames.Identifier);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyBadDirective(BadDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.Identifier, ClassificationTypeNames.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyLineDirective(LineDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.LineKeyword, ClassificationTypeNames.PreprocessorKeyword);

            switch (node.Line.Kind())
            {
                case SyntaxKind.HiddenKeyword:
                case SyntaxKind.DefaultKeyword:
                    AddClassification(node.Line, ClassificationTypeNames.PreprocessorKeyword);
                    break;
                case SyntaxKind.NumericLiteralToken:
                    AddClassification(node.Line, ClassificationTypeNames.NumericLiteral);
                    break;
            }

            if (node.File.Kind() != SyntaxKind.None)
            {
                AddClassification(node.File, ClassificationTypeNames.StringLiteral);
            }

            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyPragmaChecksumDirective(PragmaChecksumDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.PragmaKeyword, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.ChecksumKeyword, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.File, ClassificationTypeNames.StringLiteral);
            AddClassification(node.Guid, ClassificationTypeNames.StringLiteral);
            AddClassification(node.Bytes, ClassificationTypeNames.StringLiteral);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyPragmaWarningDirective(PragmaWarningDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.PragmaKeyword, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.WarningKeyword, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.DisableOrRestoreKeyword, ClassificationTypeNames.PreprocessorKeyword);

            foreach (var nodeOrToken in node.ErrorCodes.GetWithSeparators())
            {
                ClassifyNodeOrToken(nodeOrToken);
            }

            if (node.ErrorCodes.Count == 0)
            {
                // When there are no error codes, we need to classify the directive's trivia.
                // (When there are error codes, ClassifyNodeOrToken above takes care of that.)
                ClassifyDirectiveTrivia(node);
            }
        }

        private void ClassifyReferenceDirective(ReferenceDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.ReferenceKeyword, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.File, ClassificationTypeNames.StringLiteral);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyLoadDirective(LoadDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.LoadKeyword, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.File, ClassificationTypeNames.StringLiteral);
            ClassifyDirectiveTrivia(node);
        }

        private void ClassifyNullableDirective(NullableDirectiveTriviaSyntax node)
        {
            AddClassification(node.HashToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.NullableKeyword, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.SettingToken, ClassificationTypeNames.PreprocessorKeyword);
            AddClassification(node.TargetToken, ClassificationTypeNames.PreprocessorKeyword);
            ClassifyDirectiveTrivia(node);
        }
    }
}
