// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Compilers.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.Services.CSharp.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Services.Editor.CSharp.Extensions
{
    internal static class IClassificationTypesExtensions
    {
        /// <summary>
        /// Determine the classification type for a given token.
        /// </summary>
        /// <param name="classificationTypes">A classification service to retrieve classification types.</param>
        /// <param name="token">The token.</param>
        /// <param name="syntaxTree">The tree containing the token (can be null for tokens that are
        /// unparented).</param>
        /// <returns>The correct syntactic classification for the token.</returns>
        public static IClassificationType GetClassificationForToken(this IClassificationTypes classificationTypes, SyntaxToken token, SyntaxTree syntaxTree)
        {
            if (SyntaxFacts.IsKeywordKind(token.Kind))
            {
                return classificationTypes.Keyword;
            }
            else if (token.Kind.IsPunctuation())
            {
                return GetClassificationForPunctuation(classificationTypes, token);
            }
            else if (token.Kind == SyntaxKind.IdentifierToken)
            {
                return GetClassificationForIdentifer(classificationTypes, token, syntaxTree);
            }
            else if (token.Kind == SyntaxKind.StringLiteralToken || token.Kind == SyntaxKind.CharacterLiteralToken)
            {
                return token.IsVerbatimStringLiteral()
                    ? classificationTypes.VerbatimStringLiteral
                    : classificationTypes.StringLiteral;
            }
            else if (token.Kind == SyntaxKind.NumericLiteralToken)
            {
                return classificationTypes.NumericLiteral;
            }

            return null;
        }

        private static IClassificationType GetClassificationForIdentifer(IClassificationTypes classificationTypes, SyntaxToken token, SyntaxTree syntaxTree)
        {
            if (token.Parent is TypeDeclarationSyntax &&
                ((token.Parent as TypeDeclarationSyntax).Identifier == token))
            {
                return GetClassificationForTypeDeclarationIdentifier(classificationTypes, token);
            }
            else if (token.Parent is EnumDeclarationSyntax &&
                (token.Parent as EnumDeclarationSyntax).Identifier == token)
            {
                return classificationTypes.EnumTypeName;
            }
            else if (token.Parent is DelegateDeclarationSyntax &&
                (token.Parent as DelegateDeclarationSyntax).Identifier == token)
            {
                return classificationTypes.DelegateTypeName;
            }
            else if (token.Parent is TypeParameterSyntax &&
                (token.Parent as TypeParameterSyntax).Identifier == token)
            {
                return classificationTypes.TypeParameterName;
            }
            else if (syntaxTree != null && (syntaxTree.IsActualContextualKeyword(token) || syntaxTree.CouldBeVarKeywordInDeclaration(token)))
            {
                return classificationTypes.Keyword;
            }
            else
            {
                return classificationTypes.Identifier;
            }
        }

        private static IClassificationType GetClassificationForTypeDeclarationIdentifier(IClassificationTypes classificationTypes, SyntaxToken identifier)
        {
            switch (identifier.Parent.Kind)
            {
                case SyntaxKind.ClassDeclaration:
                    return classificationTypes.TypeName;
                case SyntaxKind.StructDeclaration:
                    return classificationTypes.StructureTypeName;
                case SyntaxKind.InterfaceDeclaration:
                    return classificationTypes.InterfaceTypeName;
                default:
                    return null;
            }
        }

        private static IClassificationType GetClassificationForPunctuation(IClassificationTypes classificationTypes, SyntaxToken token)
        {
            if (token.Kind.IsOperator())
            {
                // special cases...
                switch (token.Kind)
                {
                    case SyntaxKind.LessThanToken:
                    case SyntaxKind.GreaterThanToken:
                        // the < and > tokens of a type parameter list should be classified as
                        // punctuation; otherwise, they're operators.
                        if (token.Parent != null)
                        {
                            if (token.Parent.Kind == SyntaxKind.TypeParameterList ||
                                token.Parent.Kind == SyntaxKind.TypeArgumentList)
                            {
                                return classificationTypes.Punctuation;
                            }
                        }

                        break;
                    case SyntaxKind.ColonToken:
                        // the : for inheritance/implements or labels should be classified as
                        // punctuation; otherwise, it's from a conditional operator.
                        if (token.Parent != null)
                        {
                            if (token.Parent.Kind != SyntaxKind.ConditionalExpression)
                            {
                                return classificationTypes.Punctuation;
                            }
                        }

                        break;
                }

                return classificationTypes.Operator;
            }
            else
            {
                return classificationTypes.Punctuation;
            }
        }

        private static bool IsOperator(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.TildeToken:
                case SyntaxKind.ExclamationToken:
                case SyntaxKind.PercentToken:
                case SyntaxKind.CaretToken:
                case SyntaxKind.AmpersandToken:
                case SyntaxKind.AsteriskToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.PlusToken:
                case SyntaxKind.EqualsToken:
                case SyntaxKind.BarToken:
                case SyntaxKind.ColonToken:
                case SyntaxKind.LessThanToken:
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.DotToken:
                case SyntaxKind.QuestionToken:
                case SyntaxKind.SlashToken:
                case SyntaxKind.BarBarToken:
                case SyntaxKind.AmpersandAmpersandToken:
                case SyntaxKind.MinusMinusToken:
                case SyntaxKind.PlusPlusToken:
                case SyntaxKind.ColonColonToken:
                case SyntaxKind.QuestionQuestionToken:
                case SyntaxKind.MinusGreaterThanToken:
                case SyntaxKind.ExclamationEqualsToken:
                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.EqualsGreaterThanToken:
                case SyntaxKind.LessThanEqualsToken:
                case SyntaxKind.LessThanLessThanToken:
                case SyntaxKind.LessThanLessThanEqualsToken:
                case SyntaxKind.GreaterThanEqualsToken:
                case SyntaxKind.GreaterThanGreaterThanToken:
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                case SyntaxKind.SlashEqualsToken:
                case SyntaxKind.AsteriskEqualsToken:
                case SyntaxKind.BarEqualsToken:
                case SyntaxKind.AmpersandEqualsToken:
                case SyntaxKind.PlusEqualsToken:
                case SyntaxKind.MinusEqualsToken:
                case SyntaxKind.CaretEqualsToken:
                case SyntaxKind.PercentEqualsToken:
                    return true;

                default:
                    return false;
            }
        }
    }
}
