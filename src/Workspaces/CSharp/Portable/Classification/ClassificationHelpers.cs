// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    internal static class ClassificationHelpers
    {
        private const string FromKeyword = "from";
        private const string ValueKeyword = "value";
        private const string VarKeyword = "var";
        private const string AwaitKeyword = "await";

        /// <summary>
        /// Determine the classification type for a given token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>The correct syntactic classification for the token.</returns>
        public static string GetClassification(SyntaxToken token)
        {
            if (SyntaxFacts.IsKeywordKind(token.Kind()))
            {
                return ClassificationTypeNames.Keyword;
            }
            else if (SyntaxFacts.IsPunctuation(token.Kind()))
            {
                return GetClassificationForPunctuation(token);
            }
            else if (token.Kind() == SyntaxKind.IdentifierToken)
            {
                return GetClassificationForIdentifier(token);
            }
            else if (IsStringToken(token))
            {
                return IsVerbatimStringToken(token)
                    ? ClassificationTypeNames.VerbatimStringLiteral
                    : ClassificationTypeNames.StringLiteral;
            }
            else if (token.Kind() == SyntaxKind.NumericLiteralToken)
            {
                return ClassificationTypeNames.NumericLiteral;
            }

            return null;
        }

        private static bool IsStringToken(SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.StringLiteralToken)
                || token.IsKind(SyntaxKind.CharacterLiteralToken)
                || token.IsKind(SyntaxKind.InterpolatedStringStartToken)
                || token.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken)
                || token.IsKind(SyntaxKind.InterpolatedStringTextToken)
                || token.IsKind(SyntaxKind.InterpolatedStringEndToken);
        }

        private static bool IsVerbatimStringToken(SyntaxToken token)
        {
            if (token.IsVerbatimStringLiteral())
            {
                return true;
            }

            switch (token.Kind())
            {
                case SyntaxKind.InterpolatedVerbatimStringStartToken:
                    return true;
                case SyntaxKind.InterpolatedStringStartToken:
                    return false;

                case SyntaxKind.InterpolatedStringEndToken:
                    {
                        var interpolatedString = token.Parent as InterpolatedStringExpressionSyntax;

                        return interpolatedString != null
                            && interpolatedString.StringStartToken.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken);
                    }

                case SyntaxKind.InterpolatedStringTextToken:
                    {
                        var interpolatedStringText = token.Parent as InterpolatedStringTextSyntax;
                        if (interpolatedStringText == null)
                        {
                            return false;
                        }

                        var interpolatedString = interpolatedStringText.Parent as InterpolatedStringExpressionSyntax;

                        return interpolatedString != null
                            && interpolatedString.StringStartToken.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken);
                    }
            }

            return false;
        }

        private static string GetClassificationForIdentifier(SyntaxToken token)
        {
            var typeDeclaration = token.Parent as BaseTypeDeclarationSyntax;

            if (typeDeclaration != null && typeDeclaration.Identifier == token)
            {
                return GetClassificationForTypeDeclarationIdentifier(token);
            }
            else if (token.Parent.IsKind(SyntaxKind.DelegateDeclaration) && ((DelegateDeclarationSyntax)token.Parent).Identifier == token)
            {
                return ClassificationTypeNames.DelegateName;
            }
            else if (token.Parent.IsKind(SyntaxKind.TypeParameter) && ((TypeParameterSyntax)token.Parent).Identifier == token)
            {
                return ClassificationTypeNames.TypeParameterName;
            }
            else if (IsActualContextualKeyword(token))
            {
                return ClassificationTypeNames.Keyword;
            }
            else
            {
                return ClassificationTypeNames.Identifier;
            }
        }

        private static string GetClassificationForTypeDeclarationIdentifier(SyntaxToken identifier)
        {
            switch (identifier.Parent.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ClassificationTypeNames.ClassName;
                case SyntaxKind.EnumDeclaration:
                    return ClassificationTypeNames.EnumName;
                case SyntaxKind.StructDeclaration:
                    return ClassificationTypeNames.StructName;
                case SyntaxKind.InterfaceDeclaration:
                    return ClassificationTypeNames.InterfaceName;
                default:
                    return null;
            }
        }

        private static string GetClassificationForPunctuation(SyntaxToken token)
        {
            if (token.Kind().IsOperator())
            {
                // special cases...
                switch (token.Kind())
                {
                    case SyntaxKind.LessThanToken:
                    case SyntaxKind.GreaterThanToken:
                        // the < and > tokens of a type parameter list should be classified as
                        // punctuation; otherwise, they're operators.
                        if (token.Parent != null)
                        {
                            if (token.Parent.Kind() == SyntaxKind.TypeParameterList ||
                                token.Parent.Kind() == SyntaxKind.TypeArgumentList)
                            {
                                return ClassificationTypeNames.Punctuation;
                            }
                        }

                        break;
                    case SyntaxKind.ColonToken:
                        // the : for inheritance/implements or labels should be classified as
                        // punctuation; otherwise, it's from a conditional operator.
                        if (token.Parent != null)
                        {
                            if (token.Parent.Kind() != SyntaxKind.ConditionalExpression)
                            {
                                return ClassificationTypeNames.Punctuation;
                            }
                        }

                        break;
                }

                return ClassificationTypeNames.Operator;
            }
            else
            {
                return ClassificationTypeNames.Punctuation;
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

        private static bool IsActualContextualKeyword(SyntaxToken token)
        {
            if (token.Parent.IsKind(SyntaxKind.LabeledStatement))
            {
                var statement = (LabeledStatementSyntax)token.Parent;
                if (statement.Identifier == token)
                {
                    return false;
                }
            }

            // Ensure that the text and value text are the same. Otherwise, the identifier might
            // be escaped. I.e. "var", but not "@var"
            if (token.ToString() != token.ValueText)
            {
                return false;
            }

            // Standard cases.  We can just check the parent and see if we're
            // in the right position to be considered a contextual keyword
            if (token.Parent != null)
            {
                switch (token.ValueText)
                {
                    case AwaitKeyword:
                        return token.GetNextToken(includeZeroWidth: true).IsMissing;

                    case FromKeyword:
                        var fromClause = token.Parent.FirstAncestorOrSelf<FromClauseSyntax>();
                        return fromClause != null && fromClause.FromKeyword == token;

                    case VarKeyword:
                        // var
                        if (token.Parent is IdentifierNameSyntax && token.Parent?.Parent is ExpressionStatementSyntax)
                        {
                            return true;
                        }

                        // we allow var any time it looks like a variable declaration, and is not in a
                        // field or event field.
                        return
                            token.Parent is IdentifierNameSyntax &&
                            token.Parent.Parent is VariableDeclarationSyntax &&
                            !(token.Parent.Parent.Parent is FieldDeclarationSyntax) &&
                            !(token.Parent.Parent.Parent is EventFieldDeclarationSyntax);
                }
            }

            return false;
        }

        internal static void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var text2 = text.ToString(textSpan);
            var tokens = SyntaxFactory.ParseTokens(text2, initialTokenPosition: textSpan.Start);

            Worker.CollectClassifiedSpans(tokens, textSpan, result, cancellationToken);
        }

        internal static ClassifiedSpan AdjustStaleClassification(SourceText rawText, ClassifiedSpan classifiedSpan)
        {
            // If we marked this as an identifier and it should now be a keyword
            // (or vice versa), then fix this up and return it. 
            var classificationType = classifiedSpan.ClassificationType;

            // Check if the token's type has changed.  Note: we don't check for "wasPPKeyword &&
            // !isPPKeyword" here.  That's because for fault tolerance any identifier will end up
            // being parsed as a PP keyword eventually, and if we have the check here, the text
            // flickers between blue and black while typing.  See
            // http://vstfdevdiv:8080/web/wi.aspx?id=3521 for details.
            var wasKeyword = classificationType == ClassificationTypeNames.Keyword;
            var wasIdentifier = classificationType == ClassificationTypeNames.Identifier;

            // We only do this for identifiers/keywords.
            if (wasKeyword || wasIdentifier)
            {
                // Get the current text under the tag.
                var span = classifiedSpan.TextSpan;
                var text = rawText.ToString(span);

                // Now, try to find the token that corresponds to that text.  If
                // we get 0 or 2+ tokens, then we can't do anything with this.  
                // Also, if that text includes trivia, then we can't do anything.
                var token = SyntaxFactory.ParseToken(text);
                if (token.Span.Length == span.Length)
                {
                    // var and dynamic are not contextual keywords.  They are always identifiers
                    // (that we classify as keywords).  Because we are just parsing a token we don't
                    // know if we're in the right context for them to be identifiers or keywords.
                    // So, we base on decision on what they were before.  i.e. if we had a keyword
                    // before, then assume it stays a keyword if we see 'var' or 'dynamic.
                    var isKeyword = SyntaxFacts.IsKeywordKind(token.Kind())
                        || (wasKeyword && SyntaxFacts.GetContextualKeywordKind(text) != SyntaxKind.None)
                        || (wasKeyword && token.ToString() == "var")
                        || (wasKeyword && token.ToString() == "dynamic");

                    var isIdentifier = token.Kind() == SyntaxKind.IdentifierToken;

                    // We only do this for identifiers/keywords.
                    if (isKeyword || isIdentifier)
                    {
                        if ((wasKeyword && !isKeyword) ||
                            (wasIdentifier && !isIdentifier))
                        {
                            // It changed!  Return the new type of tagspan.
                            return new ClassifiedSpan(
                                isKeyword ? ClassificationTypeNames.Keyword : ClassificationTypeNames.Identifier, span);
                        }
                    }
                }
            }

            // didn't need to do anything to this one.
            return classifiedSpan;
        }
    }
}
