// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    public static partial class SyntaxFacts
    {
        public static bool IsKeywordKind(SyntaxKind kind)
        {
            return IsReservedKeyword(kind) || IsContextualKeyword(kind);
        }

        public static IEnumerable<SyntaxKind> GetReservedKeywordKinds()
        {
            for (int i = (int)SyntaxKind.BoolKeyword; i <= (int)SyntaxKind.ImplicitKeyword; i++)
            {
                yield return (SyntaxKind)i;
            }
        }

        public static IEnumerable<SyntaxKind> GetKeywordKinds()
        {
            foreach (var reserved in GetReservedKeywordKinds())
            {
                yield return reserved;
            }

            foreach (var contextual in GetContextualKeywordKinds())
            {
                yield return contextual;
            }
        }

        public static bool IsReservedKeyword(SyntaxKind kind)
        {
            return kind >= SyntaxKind.BoolKeyword && kind <= SyntaxKind.ImplicitKeyword;
        }

        public static bool IsAttributeTargetSpecifier(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.AssemblyKeyword:
                case SyntaxKind.ModuleKeyword:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsAccessibilityModifier(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PrivateKeyword:
                case SyntaxKind.ProtectedKeyword:
                case SyntaxKind.InternalKeyword:
                case SyntaxKind.PublicKeyword:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsPreprocessorKeyword(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:
                case SyntaxKind.DefaultKeyword:
                case SyntaxKind.IfKeyword:
                case SyntaxKind.ElseKeyword:
                case SyntaxKind.ElifKeyword:
                case SyntaxKind.EndIfKeyword:
                case SyntaxKind.RegionKeyword:
                case SyntaxKind.EndRegionKeyword:
                case SyntaxKind.DefineKeyword:
                case SyntaxKind.UndefKeyword:
                case SyntaxKind.WarningKeyword:
                case SyntaxKind.ErrorKeyword:
                case SyntaxKind.LineKeyword:
                case SyntaxKind.PragmaKeyword:
                case SyntaxKind.HiddenKeyword:
                case SyntaxKind.ChecksumKeyword:
                case SyntaxKind.DisableKeyword:
                case SyntaxKind.RestoreKeyword:
                case SyntaxKind.ReferenceKeyword:
                case SyntaxKind.LoadKeyword:
                case SyntaxKind.NullableKeyword:
                case SyntaxKind.EnableKeyword:
                case SyntaxKind.WarningsKeyword:
                case SyntaxKind.AnnotationsKeyword:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Some preprocessor keywords are only keywords when they appear after a
        /// hash sign (#).  For these keywords, the lexer will produce tokens with
        /// Kind = SyntaxKind.IdentifierToken and ContextualKind set to the keyword
        /// SyntaxKind.
        /// </summary>
        /// <remarks>
        /// This wrinkle is specifically not publicly exposed.
        /// </remarks>
        internal static bool IsPreprocessorContextualKeyword(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:
                case SyntaxKind.DefaultKeyword:
                case SyntaxKind.HiddenKeyword:
                case SyntaxKind.ChecksumKeyword:
                case SyntaxKind.DisableKeyword:
                case SyntaxKind.RestoreKeyword:
                case SyntaxKind.EnableKeyword:
                case SyntaxKind.WarningsKeyword:
                case SyntaxKind.AnnotationsKeyword:
                    return false;
                default:
                    return IsPreprocessorKeyword(kind);
            }
        }

        public static IEnumerable<SyntaxKind> GetPreprocessorKeywordKinds()
        {
            yield return SyntaxKind.TrueKeyword;
            yield return SyntaxKind.FalseKeyword;
            yield return SyntaxKind.DefaultKeyword;
            yield return SyntaxKind.HiddenKeyword;
            for (int i = (int)SyntaxKind.ElifKeyword; i <= (int)SyntaxKind.RestoreKeyword; i++)
            {
                yield return (SyntaxKind)i;
            }
        }

        public static bool IsPunctuation(SyntaxKind kind)
        {
            return kind >= SyntaxKind.TildeToken && kind <= SyntaxKind.PercentEqualsToken;
        }

        public static bool IsLanguagePunctuation(SyntaxKind kind)
        {
            return IsPunctuation(kind) && !IsPreprocessorKeyword(kind) && !IsDebuggerSpecialPunctuation(kind);
        }

        public static bool IsPreprocessorPunctuation(SyntaxKind kind)
        {
            return kind == SyntaxKind.HashToken;
        }

        private static bool IsDebuggerSpecialPunctuation(SyntaxKind kind)
        {
            // TODO: What about "<>f_AnonymousMethod"? Or "123#"? What's this used for?
            return kind == SyntaxKind.DollarToken;
        }

        public static IEnumerable<SyntaxKind> GetPunctuationKinds()
        {
            for (int i = (int)SyntaxKind.TildeToken; i <= (int)SyntaxKind.PercentEqualsToken; i++)
            {
                yield return (SyntaxKind)i;
            }
        }

        public static bool IsPunctuationOrKeyword(SyntaxKind kind)
        {
            return kind >= SyntaxKind.TildeToken && kind <= SyntaxKind.EndOfFileToken;
        }

        internal static bool IsLiteral(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.IdentifierToken:
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.CharacterLiteralToken:
                case SyntaxKind.NumericLiteralToken:
                case SyntaxKind.XmlTextLiteralToken:
                case SyntaxKind.XmlTextLiteralNewLineToken:
                case SyntaxKind.XmlEntityLiteralToken:
                    //case SyntaxKind.Unknown:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsAnyToken(SyntaxKind kind)
        {
            if (kind >= SyntaxKind.TildeToken && kind < SyntaxKind.EndOfLineTrivia) return true;
            switch (kind)
            {
                case SyntaxKind.InterpolatedStringToken:
                case SyntaxKind.InterpolatedStringStartToken:
                case SyntaxKind.InterpolatedVerbatimStringStartToken:
                case SyntaxKind.InterpolatedStringTextToken:
                case SyntaxKind.InterpolatedStringEndToken:
                case SyntaxKind.LoadKeyword:
                case SyntaxKind.NullableKeyword:
                case SyntaxKind.EnableKeyword:
                case SyntaxKind.UnderscoreToken:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsTrivia(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.EndOfLineTrivia:
                case SyntaxKind.WhitespaceTrivia:
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                case SyntaxKind.DisabledTextTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                case SyntaxKind.ConflictMarkerTrivia:
                    return true;
                default:
                    return IsPreprocessorDirective(kind);
            }
        }

        public static bool IsPreprocessorDirective(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.IfDirectiveTrivia:
                case SyntaxKind.ElifDirectiveTrivia:
                case SyntaxKind.ElseDirectiveTrivia:
                case SyntaxKind.EndIfDirectiveTrivia:
                case SyntaxKind.RegionDirectiveTrivia:
                case SyntaxKind.EndRegionDirectiveTrivia:
                case SyntaxKind.DefineDirectiveTrivia:
                case SyntaxKind.UndefDirectiveTrivia:
                case SyntaxKind.ErrorDirectiveTrivia:
                case SyntaxKind.WarningDirectiveTrivia:
                case SyntaxKind.LineDirectiveTrivia:
                case SyntaxKind.PragmaWarningDirectiveTrivia:
                case SyntaxKind.PragmaChecksumDirectiveTrivia:
                case SyntaxKind.ReferenceDirectiveTrivia:
                case SyntaxKind.LoadDirectiveTrivia:
                case SyntaxKind.BadDirectiveTrivia:
                case SyntaxKind.ShebangDirectiveTrivia:
                case SyntaxKind.NullableDirectiveTrivia:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsName(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.IdentifierName:
                case SyntaxKind.GenericName:
                case SyntaxKind.QualifiedName:
                case SyntaxKind.AliasQualifiedName:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsPredefinedType(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.BoolKeyword:
                case SyntaxKind.ByteKeyword:
                case SyntaxKind.SByteKeyword:
                case SyntaxKind.IntKeyword:
                case SyntaxKind.UIntKeyword:
                case SyntaxKind.ShortKeyword:
                case SyntaxKind.UShortKeyword:
                case SyntaxKind.LongKeyword:
                case SyntaxKind.ULongKeyword:
                case SyntaxKind.FloatKeyword:
                case SyntaxKind.DoubleKeyword:
                case SyntaxKind.DecimalKeyword:
                case SyntaxKind.StringKeyword:
                case SyntaxKind.CharKeyword:
                case SyntaxKind.ObjectKeyword:
                case SyntaxKind.VoidKeyword:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsTypeSyntax(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.ArrayType:
                case SyntaxKind.PointerType:
                case SyntaxKind.NullableType:
                case SyntaxKind.PredefinedType:
                case SyntaxKind.TupleType:
                    return true;
                default:
                    return IsName(kind);
            }
        }

        /// <summary>
        /// Member declarations that can appear in global code (other than type declarations).
        /// </summary>
        public static bool IsGlobalMemberDeclaration(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.GlobalStatement:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                    return true;
            }
            return false;
        }

        public static bool IsTypeDeclaration(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.EnumDeclaration:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsNamespaceMemberDeclaration(SyntaxKind kind)
            => IsTypeDeclaration(kind) || (kind == SyntaxKind.NamespaceDeclaration);

        public static bool IsAnyUnaryExpression(SyntaxKind token)
        {
            return IsPrefixUnaryExpression(token) || IsPostfixUnaryExpression(token);
        }

        public static bool IsPrefixUnaryExpression(SyntaxKind token)
        {
            return GetPrefixUnaryExpression(token) != SyntaxKind.None;
        }

        public static bool IsPrefixUnaryExpressionOperatorToken(SyntaxKind token)
        {
            return GetPrefixUnaryExpression(token) != SyntaxKind.None;
        }

        public static SyntaxKind GetPrefixUnaryExpression(SyntaxKind token)
        {
            switch (token)
            {
                case SyntaxKind.PlusToken:
                    return SyntaxKind.UnaryPlusExpression;
                case SyntaxKind.MinusToken:
                    return SyntaxKind.UnaryMinusExpression;
                case SyntaxKind.TildeToken:
                    return SyntaxKind.BitwiseNotExpression;
                case SyntaxKind.ExclamationToken:
                    return SyntaxKind.LogicalNotExpression;
                case SyntaxKind.PlusPlusToken:
                    return SyntaxKind.PreIncrementExpression;
                case SyntaxKind.MinusMinusToken:
                    return SyntaxKind.PreDecrementExpression;
                case SyntaxKind.AmpersandToken:
                    return SyntaxKind.AddressOfExpression;
                case SyntaxKind.AsteriskToken:
                    return SyntaxKind.PointerIndirectionExpression;
                case SyntaxKind.CaretToken:
                    return SyntaxKind.IndexExpression;
                default:
                    return SyntaxKind.None;
            }
        }

        public static bool IsPostfixUnaryExpression(SyntaxKind token)
        {
            return GetPostfixUnaryExpression(token) != SyntaxKind.None;
        }

        public static bool IsPostfixUnaryExpressionToken(SyntaxKind token)
        {
            return GetPostfixUnaryExpression(token) != SyntaxKind.None;
        }

        public static SyntaxKind GetPostfixUnaryExpression(SyntaxKind token)
        {
            switch (token)
            {
                case SyntaxKind.PlusPlusToken:
                    return SyntaxKind.PostIncrementExpression;
                case SyntaxKind.MinusMinusToken:
                    return SyntaxKind.PostDecrementExpression;
                case SyntaxKind.ExclamationToken:
                    return SyntaxKind.SuppressNullableWarningExpression;
                default:
                    return SyntaxKind.None;
            }
        }

        internal static bool IsIncrementOrDecrementOperator(SyntaxKind token)
        {
            switch (token)
            {
                case SyntaxKind.PlusPlusToken:
                case SyntaxKind.MinusMinusToken:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsUnaryOperatorDeclarationToken(SyntaxKind token)
        {
            return IsPrefixUnaryExpressionOperatorToken(token) ||
                   token == SyntaxKind.TrueKeyword ||
                   token == SyntaxKind.FalseKeyword;
        }

        public static bool IsAnyOverloadableOperator(SyntaxKind kind)
        {
            return IsOverloadableBinaryOperator(kind) || IsOverloadableUnaryOperator(kind);
        }

        public static bool IsOverloadableBinaryOperator(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.AsteriskToken:
                case SyntaxKind.SlashToken:
                case SyntaxKind.PercentToken:
                case SyntaxKind.CaretToken:
                case SyntaxKind.AmpersandToken:
                case SyntaxKind.BarToken:
                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.LessThanToken:
                case SyntaxKind.LessThanEqualsToken:
                case SyntaxKind.LessThanLessThanToken:
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.GreaterThanEqualsToken:
                case SyntaxKind.GreaterThanGreaterThanToken:
                case SyntaxKind.ExclamationEqualsToken:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsOverloadableUnaryOperator(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.TildeToken:
                case SyntaxKind.ExclamationToken:
                case SyntaxKind.PlusPlusToken:
                case SyntaxKind.MinusMinusToken:
                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsPrimaryFunction(SyntaxKind keyword)
        {
            return GetPrimaryFunction(keyword) != SyntaxKind.None;
        }

        public static SyntaxKind GetPrimaryFunction(SyntaxKind keyword)
        {
            switch (keyword)
            {
                case SyntaxKind.MakeRefKeyword:
                    return SyntaxKind.MakeRefExpression;
                case SyntaxKind.RefTypeKeyword:
                    return SyntaxKind.RefTypeExpression;
                case SyntaxKind.RefValueKeyword:
                    return SyntaxKind.RefValueExpression;
                case SyntaxKind.CheckedKeyword:
                    return SyntaxKind.CheckedExpression;
                case SyntaxKind.UncheckedKeyword:
                    return SyntaxKind.UncheckedExpression;
                case SyntaxKind.DefaultKeyword:
                    return SyntaxKind.DefaultExpression;
                case SyntaxKind.TypeOfKeyword:
                    return SyntaxKind.TypeOfExpression;
                case SyntaxKind.SizeOfKeyword:
                    return SyntaxKind.SizeOfExpression;
                default:
                    return SyntaxKind.None;
            }
        }

        public static bool IsLiteralExpression(SyntaxKind token)
        {
            return GetLiteralExpression(token) != SyntaxKind.None;
        }

        public static SyntaxKind GetLiteralExpression(SyntaxKind token)
        {
            switch (token)
            {
                case SyntaxKind.StringLiteralToken:
                    return SyntaxKind.StringLiteralExpression;
                case SyntaxKind.CharacterLiteralToken:
                    return SyntaxKind.CharacterLiteralExpression;
                case SyntaxKind.NumericLiteralToken:
                    return SyntaxKind.NumericLiteralExpression;
                case SyntaxKind.NullKeyword:
                    return SyntaxKind.NullLiteralExpression;
                case SyntaxKind.TrueKeyword:
                    return SyntaxKind.TrueLiteralExpression;
                case SyntaxKind.FalseKeyword:
                    return SyntaxKind.FalseLiteralExpression;
                case SyntaxKind.ArgListKeyword:
                    return SyntaxKind.ArgListExpression;
                default:
                    return SyntaxKind.None;
            }
        }

        public static bool IsInstanceExpression(SyntaxKind token)
        {
            return GetInstanceExpression(token) != SyntaxKind.None;
        }

        public static SyntaxKind GetInstanceExpression(SyntaxKind token)
        {
            switch (token)
            {
                case SyntaxKind.ThisKeyword:
                    return SyntaxKind.ThisExpression;
                case SyntaxKind.BaseKeyword:
                    return SyntaxKind.BaseExpression;
                default:
                    return SyntaxKind.None;
            }
        }

        public static bool IsBinaryExpression(SyntaxKind token)
        {
            return GetBinaryExpression(token) != SyntaxKind.None;
        }

        public static bool IsBinaryExpressionOperatorToken(SyntaxKind token)
        {
            return GetBinaryExpression(token) != SyntaxKind.None;
        }

        public static SyntaxKind GetBinaryExpression(SyntaxKind token)
        {
            switch (token)
            {
                case SyntaxKind.QuestionQuestionToken:
                    return SyntaxKind.CoalesceExpression;
                case SyntaxKind.IsKeyword:
                    return SyntaxKind.IsExpression;
                case SyntaxKind.AsKeyword:
                    return SyntaxKind.AsExpression;
                case SyntaxKind.BarToken:
                    return SyntaxKind.BitwiseOrExpression;
                case SyntaxKind.CaretToken:
                    return SyntaxKind.ExclusiveOrExpression;
                case SyntaxKind.AmpersandToken:
                    return SyntaxKind.BitwiseAndExpression;
                case SyntaxKind.EqualsEqualsToken:
                    return SyntaxKind.EqualsExpression;
                case SyntaxKind.ExclamationEqualsToken:
                    return SyntaxKind.NotEqualsExpression;
                case SyntaxKind.LessThanToken:
                    return SyntaxKind.LessThanExpression;
                case SyntaxKind.LessThanEqualsToken:
                    return SyntaxKind.LessThanOrEqualExpression;
                case SyntaxKind.GreaterThanToken:
                    return SyntaxKind.GreaterThanExpression;
                case SyntaxKind.GreaterThanEqualsToken:
                    return SyntaxKind.GreaterThanOrEqualExpression;
                case SyntaxKind.LessThanLessThanToken:
                    return SyntaxKind.LeftShiftExpression;
                case SyntaxKind.GreaterThanGreaterThanToken:
                    return SyntaxKind.RightShiftExpression;
                case SyntaxKind.PlusToken:
                    return SyntaxKind.AddExpression;
                case SyntaxKind.MinusToken:
                    return SyntaxKind.SubtractExpression;
                case SyntaxKind.AsteriskToken:
                    return SyntaxKind.MultiplyExpression;
                case SyntaxKind.SlashToken:
                    return SyntaxKind.DivideExpression;
                case SyntaxKind.PercentToken:
                    return SyntaxKind.ModuloExpression;
                case SyntaxKind.AmpersandAmpersandToken:
                    return SyntaxKind.LogicalAndExpression;
                case SyntaxKind.BarBarToken:
                    return SyntaxKind.LogicalOrExpression;
                default:
                    return SyntaxKind.None;
            }
        }

        public static bool IsAssignmentExpression(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.CoalesceAssignmentExpression:
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.RightShiftAssignmentExpression:
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.ModuloAssignmentExpression:
                case SyntaxKind.SimpleAssignmentExpression:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsAssignmentExpressionOperatorToken(SyntaxKind token)
        {
            switch (token)
            {
                case SyntaxKind.QuestionQuestionEqualsToken:
                case SyntaxKind.BarEqualsToken:
                case SyntaxKind.AmpersandEqualsToken:
                case SyntaxKind.CaretEqualsToken:
                case SyntaxKind.LessThanLessThanEqualsToken:
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                case SyntaxKind.PlusEqualsToken:
                case SyntaxKind.MinusEqualsToken:
                case SyntaxKind.AsteriskEqualsToken:
                case SyntaxKind.SlashEqualsToken:
                case SyntaxKind.PercentEqualsToken:
                case SyntaxKind.EqualsToken:
                    return true;
                default:
                    return false;
            }
        }

        public static SyntaxKind GetAssignmentExpression(SyntaxKind token)
        {
            switch (token)
            {
                case SyntaxKind.BarEqualsToken:
                    return SyntaxKind.OrAssignmentExpression;
                case SyntaxKind.AmpersandEqualsToken:
                    return SyntaxKind.AndAssignmentExpression;
                case SyntaxKind.CaretEqualsToken:
                    return SyntaxKind.ExclusiveOrAssignmentExpression;
                case SyntaxKind.LessThanLessThanEqualsToken:
                    return SyntaxKind.LeftShiftAssignmentExpression;
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                    return SyntaxKind.RightShiftAssignmentExpression;
                case SyntaxKind.PlusEqualsToken:
                    return SyntaxKind.AddAssignmentExpression;
                case SyntaxKind.MinusEqualsToken:
                    return SyntaxKind.SubtractAssignmentExpression;
                case SyntaxKind.AsteriskEqualsToken:
                    return SyntaxKind.MultiplyAssignmentExpression;
                case SyntaxKind.SlashEqualsToken:
                    return SyntaxKind.DivideAssignmentExpression;
                case SyntaxKind.PercentEqualsToken:
                    return SyntaxKind.ModuloAssignmentExpression;
                case SyntaxKind.EqualsToken:
                    return SyntaxKind.SimpleAssignmentExpression;
                case SyntaxKind.QuestionQuestionEqualsToken:
                    return SyntaxKind.CoalesceAssignmentExpression;
                default:
                    return SyntaxKind.None;
            }
        }

        public static SyntaxKind GetCheckStatement(SyntaxKind keyword)
        {
            switch (keyword)
            {
                case SyntaxKind.CheckedKeyword:
                    return SyntaxKind.CheckedStatement;
                case SyntaxKind.UncheckedKeyword:
                    return SyntaxKind.UncheckedStatement;
                default:
                    return SyntaxKind.None;
            }
        }

        public static SyntaxKind GetAccessorDeclarationKind(SyntaxKind keyword)
        {
            switch (keyword)
            {
                case SyntaxKind.GetKeyword:
                    return SyntaxKind.GetAccessorDeclaration;
                case SyntaxKind.SetKeyword:
                    return SyntaxKind.SetAccessorDeclaration;
                case SyntaxKind.AddKeyword:
                    return SyntaxKind.AddAccessorDeclaration;
                case SyntaxKind.RemoveKeyword:
                    return SyntaxKind.RemoveAccessorDeclaration;
                default:
                    return SyntaxKind.None;
            }
        }

        public static bool IsAccessorDeclaration(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsAccessorDeclarationKeyword(SyntaxKind keyword)
        {
            switch (keyword)
            {
                case SyntaxKind.GetKeyword:
                case SyntaxKind.SetKeyword:
                case SyntaxKind.AddKeyword:
                case SyntaxKind.RemoveKeyword:
                    return true;
                default:
                    return false;
            }
        }

        public static SyntaxKind GetSwitchLabelKind(SyntaxKind keyword)
        {
            switch (keyword)
            {
                case SyntaxKind.CaseKeyword:
                    return SyntaxKind.CaseSwitchLabel;
                case SyntaxKind.DefaultKeyword:
                    return SyntaxKind.DefaultSwitchLabel;
                default:
                    return SyntaxKind.None;
            }
        }

        public static SyntaxKind GetBaseTypeDeclarationKind(SyntaxKind kind)
        {
            return kind == SyntaxKind.EnumKeyword ? SyntaxKind.EnumDeclaration : GetTypeDeclarationKind(kind);
        }

        public static SyntaxKind GetTypeDeclarationKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.ClassKeyword:
                    return SyntaxKind.ClassDeclaration;
                case SyntaxKind.StructKeyword:
                    return SyntaxKind.StructDeclaration;
                case SyntaxKind.InterfaceKeyword:
                    return SyntaxKind.InterfaceDeclaration;
                default:
                    return SyntaxKind.None;
            }
        }

        public static SyntaxKind GetKeywordKind(string text)
        {
            switch (text)
            {
                case "bool":
                    return SyntaxKind.BoolKeyword;
                case "byte":
                    return SyntaxKind.ByteKeyword;
                case "sbyte":
                    return SyntaxKind.SByteKeyword;
                case "short":
                    return SyntaxKind.ShortKeyword;
                case "ushort":
                    return SyntaxKind.UShortKeyword;
                case "int":
                    return SyntaxKind.IntKeyword;
                case "uint":
                    return SyntaxKind.UIntKeyword;
                case "long":
                    return SyntaxKind.LongKeyword;
                case "ulong":
                    return SyntaxKind.ULongKeyword;
                case "double":
                    return SyntaxKind.DoubleKeyword;
                case "float":
                    return SyntaxKind.FloatKeyword;
                case "decimal":
                    return SyntaxKind.DecimalKeyword;
                case "string":
                    return SyntaxKind.StringKeyword;
                case "char":
                    return SyntaxKind.CharKeyword;
                case "void":
                    return SyntaxKind.VoidKeyword;
                case "object":
                    return SyntaxKind.ObjectKeyword;
                case "typeof":
                    return SyntaxKind.TypeOfKeyword;
                case "sizeof":
                    return SyntaxKind.SizeOfKeyword;
                case "null":
                    return SyntaxKind.NullKeyword;
                case "true":
                    return SyntaxKind.TrueKeyword;
                case "false":
                    return SyntaxKind.FalseKeyword;
                case "if":
                    return SyntaxKind.IfKeyword;
                case "else":
                    return SyntaxKind.ElseKeyword;
                case "while":
                    return SyntaxKind.WhileKeyword;
                case "for":
                    return SyntaxKind.ForKeyword;
                case "foreach":
                    return SyntaxKind.ForEachKeyword;
                case "do":
                    return SyntaxKind.DoKeyword;
                case "switch":
                    return SyntaxKind.SwitchKeyword;
                case "case":
                    return SyntaxKind.CaseKeyword;
                case "default":
                    return SyntaxKind.DefaultKeyword;
                case "lock":
                    return SyntaxKind.LockKeyword;
                case "try":
                    return SyntaxKind.TryKeyword;
                case "throw":
                    return SyntaxKind.ThrowKeyword;
                case "catch":
                    return SyntaxKind.CatchKeyword;
                case "finally":
                    return SyntaxKind.FinallyKeyword;
                case "goto":
                    return SyntaxKind.GotoKeyword;
                case "break":
                    return SyntaxKind.BreakKeyword;
                case "continue":
                    return SyntaxKind.ContinueKeyword;
                case "return":
                    return SyntaxKind.ReturnKeyword;
                case "public":
                    return SyntaxKind.PublicKeyword;
                case "private":
                    return SyntaxKind.PrivateKeyword;
                case "internal":
                    return SyntaxKind.InternalKeyword;
                case "protected":
                    return SyntaxKind.ProtectedKeyword;
                case "static":
                    return SyntaxKind.StaticKeyword;
                case "readonly":
                    return SyntaxKind.ReadOnlyKeyword;
                case "sealed":
                    return SyntaxKind.SealedKeyword;
                case "const":
                    return SyntaxKind.ConstKeyword;
                case "fixed":
                    return SyntaxKind.FixedKeyword;
                case "stackalloc":
                    return SyntaxKind.StackAllocKeyword;
                case "volatile":
                    return SyntaxKind.VolatileKeyword;
                case "new":
                    return SyntaxKind.NewKeyword;
                case "override":
                    return SyntaxKind.OverrideKeyword;
                case "abstract":
                    return SyntaxKind.AbstractKeyword;
                case "virtual":
                    return SyntaxKind.VirtualKeyword;
                case "event":
                    return SyntaxKind.EventKeyword;
                case "extern":
                    return SyntaxKind.ExternKeyword;
                case "ref":
                    return SyntaxKind.RefKeyword;
                case "out":
                    return SyntaxKind.OutKeyword;
                case "in":
                    return SyntaxKind.InKeyword;
                case "is":
                    return SyntaxKind.IsKeyword;
                case "as":
                    return SyntaxKind.AsKeyword;
                case "params":
                    return SyntaxKind.ParamsKeyword;
                case "__arglist":
                    return SyntaxKind.ArgListKeyword;
                case "__makeref":
                    return SyntaxKind.MakeRefKeyword;
                case "__reftype":
                    return SyntaxKind.RefTypeKeyword;
                case "__refvalue":
                    return SyntaxKind.RefValueKeyword;
                case "this":
                    return SyntaxKind.ThisKeyword;
                case "base":
                    return SyntaxKind.BaseKeyword;
                case "namespace":
                    return SyntaxKind.NamespaceKeyword;
                case "using":
                    return SyntaxKind.UsingKeyword;
                case "class":
                    return SyntaxKind.ClassKeyword;
                case "struct":
                    return SyntaxKind.StructKeyword;
                case "interface":
                    return SyntaxKind.InterfaceKeyword;
                case "enum":
                    return SyntaxKind.EnumKeyword;
                case "delegate":
                    return SyntaxKind.DelegateKeyword;
                case "checked":
                    return SyntaxKind.CheckedKeyword;
                case "unchecked":
                    return SyntaxKind.UncheckedKeyword;
                case "unsafe":
                    return SyntaxKind.UnsafeKeyword;
                case "operator":
                    return SyntaxKind.OperatorKeyword;
                case "implicit":
                    return SyntaxKind.ImplicitKeyword;
                case "explicit":
                    return SyntaxKind.ExplicitKeyword;
                default:
                    return SyntaxKind.None;
            }
        }

        public static SyntaxKind GetOperatorKind(string operatorMetadataName)
        {
            switch (operatorMetadataName)
            {
                case WellKnownMemberNames.AdditionOperatorName: return SyntaxKind.PlusToken;
                case WellKnownMemberNames.BitwiseAndOperatorName: return SyntaxKind.AmpersandToken;
                case WellKnownMemberNames.BitwiseOrOperatorName: return SyntaxKind.BarToken;
                // case WellKnownMemberNames.ConcatenateOperatorName:
                case WellKnownMemberNames.DecrementOperatorName: return SyntaxKind.MinusMinusToken;
                case WellKnownMemberNames.DivisionOperatorName: return SyntaxKind.SlashToken;
                case WellKnownMemberNames.EqualityOperatorName: return SyntaxKind.EqualsEqualsToken;
                case WellKnownMemberNames.ExclusiveOrOperatorName: return SyntaxKind.CaretToken;
                case WellKnownMemberNames.ExplicitConversionName: return SyntaxKind.ExplicitKeyword;
                // case WellKnownMemberNames.ExponentOperatorName:
                case WellKnownMemberNames.FalseOperatorName: return SyntaxKind.FalseKeyword;
                case WellKnownMemberNames.GreaterThanOperatorName: return SyntaxKind.GreaterThanToken;
                case WellKnownMemberNames.GreaterThanOrEqualOperatorName: return SyntaxKind.GreaterThanEqualsToken;
                case WellKnownMemberNames.ImplicitConversionName: return SyntaxKind.ImplicitKeyword;
                case WellKnownMemberNames.IncrementOperatorName: return SyntaxKind.PlusPlusToken;
                case WellKnownMemberNames.InequalityOperatorName: return SyntaxKind.ExclamationEqualsToken;
                //case WellKnownMemberNames.IntegerDivisionOperatorName: 
                case WellKnownMemberNames.LeftShiftOperatorName: return SyntaxKind.LessThanLessThanToken;
                case WellKnownMemberNames.LessThanOperatorName: return SyntaxKind.LessThanToken;
                case WellKnownMemberNames.LessThanOrEqualOperatorName: return SyntaxKind.LessThanEqualsToken;
                // case WellKnownMemberNames.LikeOperatorName:
                case WellKnownMemberNames.LogicalNotOperatorName: return SyntaxKind.ExclamationToken;
                case WellKnownMemberNames.ModulusOperatorName: return SyntaxKind.PercentToken;
                case WellKnownMemberNames.MultiplyOperatorName: return SyntaxKind.AsteriskToken;
                case WellKnownMemberNames.OnesComplementOperatorName: return SyntaxKind.TildeToken;
                case WellKnownMemberNames.RightShiftOperatorName: return SyntaxKind.GreaterThanGreaterThanToken;
                case WellKnownMemberNames.SubtractionOperatorName: return SyntaxKind.MinusToken;
                case WellKnownMemberNames.TrueOperatorName: return SyntaxKind.TrueKeyword;
                case WellKnownMemberNames.UnaryNegationOperatorName: return SyntaxKind.MinusToken;
                case WellKnownMemberNames.UnaryPlusOperatorName: return SyntaxKind.PlusToken;
                default:
                    return SyntaxKind.None;
            }
        }

        public static SyntaxKind GetPreprocessorKeywordKind(string text)
        {
            switch (text)
            {
                case "true":
                    return SyntaxKind.TrueKeyword;
                case "false":
                    return SyntaxKind.FalseKeyword;
                case "default":
                    return SyntaxKind.DefaultKeyword;
                case "if":
                    return SyntaxKind.IfKeyword;
                case "else":
                    return SyntaxKind.ElseKeyword;
                case "elif":
                    return SyntaxKind.ElifKeyword;
                case "endif":
                    return SyntaxKind.EndIfKeyword;
                case "region":
                    return SyntaxKind.RegionKeyword;
                case "endregion":
                    return SyntaxKind.EndRegionKeyword;
                case "define":
                    return SyntaxKind.DefineKeyword;
                case "undef":
                    return SyntaxKind.UndefKeyword;
                case "warning":
                    return SyntaxKind.WarningKeyword;
                case "error":
                    return SyntaxKind.ErrorKeyword;
                case "line":
                    return SyntaxKind.LineKeyword;
                case "pragma":
                    return SyntaxKind.PragmaKeyword;
                case "hidden":
                    return SyntaxKind.HiddenKeyword;
                case "checksum":
                    return SyntaxKind.ChecksumKeyword;
                case "disable":
                    return SyntaxKind.DisableKeyword;
                case "restore":
                    return SyntaxKind.RestoreKeyword;
                case "r":
                    return SyntaxKind.ReferenceKeyword;
                case "load":
                    return SyntaxKind.LoadKeyword;
                case "nullable":
                    return SyntaxKind.NullableKeyword;
                case "enable":
                    return SyntaxKind.EnableKeyword;
                case "warnings":
                    return SyntaxKind.WarningsKeyword;
                case "annotations":
                    return SyntaxKind.AnnotationsKeyword;
                default:
                    return SyntaxKind.None;
            }
        }

        public static IEnumerable<SyntaxKind> GetContextualKeywordKinds()
        {
            for (int i = (int)SyntaxKind.YieldKeyword; i <= (int)SyntaxKind.NotKeyword; i++)
            {
                yield return (SyntaxKind)i;
            }
        }

        public static bool IsContextualKeyword(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.YieldKeyword:
                case SyntaxKind.PartialKeyword:
                case SyntaxKind.FromKeyword:
                case SyntaxKind.GroupKeyword:
                case SyntaxKind.JoinKeyword:
                case SyntaxKind.IntoKeyword:
                case SyntaxKind.LetKeyword:
                case SyntaxKind.ByKeyword:
                case SyntaxKind.WhereKeyword:
                case SyntaxKind.SelectKeyword:
                case SyntaxKind.GetKeyword:
                case SyntaxKind.SetKeyword:
                case SyntaxKind.AddKeyword:
                case SyntaxKind.RemoveKeyword:
                case SyntaxKind.OrderByKeyword:
                case SyntaxKind.AliasKeyword:
                case SyntaxKind.OnKeyword:
                case SyntaxKind.EqualsKeyword:
                case SyntaxKind.AscendingKeyword:
                case SyntaxKind.DescendingKeyword:
                case SyntaxKind.AssemblyKeyword:
                case SyntaxKind.ModuleKeyword:
                case SyntaxKind.TypeKeyword:
                case SyntaxKind.GlobalKeyword:
                case SyntaxKind.FieldKeyword:
                case SyntaxKind.MethodKeyword:
                case SyntaxKind.ParamKeyword:
                case SyntaxKind.PropertyKeyword:
                case SyntaxKind.TypeVarKeyword:
                case SyntaxKind.NameOfKeyword:
                case SyntaxKind.AsyncKeyword:
                case SyntaxKind.AwaitKeyword:
                case SyntaxKind.WhenKeyword:
                case SyntaxKind.UnderscoreToken:
                case SyntaxKind.VarKeyword:
                case SyntaxKind.OrKeyword:
                case SyntaxKind.AndKeyword:
                case SyntaxKind.NotKeyword:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsQueryContextualKeyword(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.FromKeyword:
                case SyntaxKind.WhereKeyword:
                case SyntaxKind.SelectKeyword:
                case SyntaxKind.GroupKeyword:
                case SyntaxKind.IntoKeyword:
                case SyntaxKind.OrderByKeyword:
                case SyntaxKind.JoinKeyword:
                case SyntaxKind.LetKeyword:
                case SyntaxKind.OnKeyword:
                case SyntaxKind.EqualsKeyword:
                case SyntaxKind.ByKeyword:
                case SyntaxKind.AscendingKeyword:
                case SyntaxKind.DescendingKeyword:
                    return true;
                default:
                    return false;
            }
        }

        public static SyntaxKind GetContextualKeywordKind(string text)
        {
            switch (text)
            {
                case "yield":
                    return SyntaxKind.YieldKeyword;
                case "partial":
                    return SyntaxKind.PartialKeyword;
                case "from":
                    return SyntaxKind.FromKeyword;
                case "group":
                    return SyntaxKind.GroupKeyword;
                case "join":
                    return SyntaxKind.JoinKeyword;
                case "into":
                    return SyntaxKind.IntoKeyword;
                case "let":
                    return SyntaxKind.LetKeyword;
                case "by":
                    return SyntaxKind.ByKeyword;
                case "where":
                    return SyntaxKind.WhereKeyword;
                case "select":
                    return SyntaxKind.SelectKeyword;
                case "get":
                    return SyntaxKind.GetKeyword;
                case "set":
                    return SyntaxKind.SetKeyword;
                case "add":
                    return SyntaxKind.AddKeyword;
                case "remove":
                    return SyntaxKind.RemoveKeyword;
                case "orderby":
                    return SyntaxKind.OrderByKeyword;
                case "alias":
                    return SyntaxKind.AliasKeyword;
                case "on":
                    return SyntaxKind.OnKeyword;
                case "equals":
                    return SyntaxKind.EqualsKeyword;
                case "ascending":
                    return SyntaxKind.AscendingKeyword;
                case "descending":
                    return SyntaxKind.DescendingKeyword;
                case "assembly":
                    return SyntaxKind.AssemblyKeyword;
                case "module":
                    return SyntaxKind.ModuleKeyword;
                case "type":
                    return SyntaxKind.TypeKeyword;
                case "field":
                    return SyntaxKind.FieldKeyword;
                case "method":
                    return SyntaxKind.MethodKeyword;
                case "param":
                    return SyntaxKind.ParamKeyword;
                case "property":
                    return SyntaxKind.PropertyKeyword;
                case "typevar":
                    return SyntaxKind.TypeVarKeyword;
                case "global":
                    return SyntaxKind.GlobalKeyword;
                case "async":
                    return SyntaxKind.AsyncKeyword;
                case "await":
                    return SyntaxKind.AwaitKeyword;
                case "when":
                    return SyntaxKind.WhenKeyword;
                case "nameof":
                    return SyntaxKind.NameOfKeyword;
                case "_":
                    return SyntaxKind.UnderscoreToken;
                case "var":
                    return SyntaxKind.VarKeyword;
                default:
                    return SyntaxKind.None;
            }
        }

        public static string GetText(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.TildeToken:
                    return "~";
                case SyntaxKind.ExclamationToken:
                    return "!";
                case SyntaxKind.DollarToken:
                    return "$";
                case SyntaxKind.PercentToken:
                    return "%";
                case SyntaxKind.CaretToken:
                    return "^";
                case SyntaxKind.AmpersandToken:
                    return "&";
                case SyntaxKind.AsteriskToken:
                    return "*";
                case SyntaxKind.OpenParenToken:
                    return "(";
                case SyntaxKind.CloseParenToken:
                    return ")";
                case SyntaxKind.MinusToken:
                    return "-";
                case SyntaxKind.PlusToken:
                    return "+";
                case SyntaxKind.EqualsToken:
                    return "=";
                case SyntaxKind.OpenBraceToken:
                    return "{";
                case SyntaxKind.CloseBraceToken:
                    return "}";
                case SyntaxKind.OpenBracketToken:
                    return "[";
                case SyntaxKind.CloseBracketToken:
                    return "]";
                case SyntaxKind.BarToken:
                    return "|";
                case SyntaxKind.BackslashToken:
                    return "\\";
                case SyntaxKind.ColonToken:
                    return ":";
                case SyntaxKind.SemicolonToken:
                    return ";";
                case SyntaxKind.DoubleQuoteToken:
                    return "\"";
                case SyntaxKind.SingleQuoteToken:
                    return "'";
                case SyntaxKind.LessThanToken:
                    return "<";
                case SyntaxKind.CommaToken:
                    return ",";
                case SyntaxKind.GreaterThanToken:
                    return ">";
                case SyntaxKind.DotToken:
                    return ".";
                case SyntaxKind.QuestionToken:
                    return "?";
                case SyntaxKind.HashToken:
                    return "#";
                case SyntaxKind.SlashToken:
                    return "/";
                case SyntaxKind.SlashGreaterThanToken:
                    return "/>";
                case SyntaxKind.LessThanSlashToken:
                    return "</";
                case SyntaxKind.XmlCommentStartToken:
                    return "<!--";
                case SyntaxKind.XmlCommentEndToken:
                    return "-->";
                case SyntaxKind.XmlCDataStartToken:
                    return "<![CDATA[";
                case SyntaxKind.XmlCDataEndToken:
                    return "]]>";
                case SyntaxKind.XmlProcessingInstructionStartToken:
                    return "<?";
                case SyntaxKind.XmlProcessingInstructionEndToken:
                    return "?>";

                // compound
                case SyntaxKind.BarBarToken:
                    return "||";
                case SyntaxKind.AmpersandAmpersandToken:
                    return "&&";
                case SyntaxKind.MinusMinusToken:
                    return "--";
                case SyntaxKind.PlusPlusToken:
                    return "++";
                case SyntaxKind.ColonColonToken:
                    return "::";
                case SyntaxKind.QuestionQuestionToken:
                    return "??";
                case SyntaxKind.MinusGreaterThanToken:
                    return "->";
                case SyntaxKind.ExclamationEqualsToken:
                    return "!=";
                case SyntaxKind.EqualsEqualsToken:
                    return "==";
                case SyntaxKind.EqualsGreaterThanToken:
                    return "=>";
                case SyntaxKind.LessThanEqualsToken:
                    return "<=";
                case SyntaxKind.LessThanLessThanToken:
                    return "<<";
                case SyntaxKind.LessThanLessThanEqualsToken:
                    return "<<=";
                case SyntaxKind.GreaterThanEqualsToken:
                    return ">=";
                case SyntaxKind.GreaterThanGreaterThanToken:
                    return ">>";
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                    return ">>=";
                case SyntaxKind.SlashEqualsToken:
                    return "/=";
                case SyntaxKind.AsteriskEqualsToken:
                    return "*=";
                case SyntaxKind.BarEqualsToken:
                    return "|=";
                case SyntaxKind.AmpersandEqualsToken:
                    return "&=";
                case SyntaxKind.PlusEqualsToken:
                    return "+=";
                case SyntaxKind.MinusEqualsToken:
                    return "-=";
                case SyntaxKind.CaretEqualsToken:
                    return "^=";
                case SyntaxKind.PercentEqualsToken:
                    return "%=";
                case SyntaxKind.QuestionQuestionEqualsToken:
                    return "??=";
                case SyntaxKind.DotDotToken:
                    return "..";

                // Keywords
                case SyntaxKind.BoolKeyword:
                    return "bool";
                case SyntaxKind.ByteKeyword:
                    return "byte";
                case SyntaxKind.SByteKeyword:
                    return "sbyte";
                case SyntaxKind.ShortKeyword:
                    return "short";
                case SyntaxKind.UShortKeyword:
                    return "ushort";
                case SyntaxKind.IntKeyword:
                    return "int";
                case SyntaxKind.UIntKeyword:
                    return "uint";
                case SyntaxKind.LongKeyword:
                    return "long";
                case SyntaxKind.ULongKeyword:
                    return "ulong";
                case SyntaxKind.DoubleKeyword:
                    return "double";
                case SyntaxKind.FloatKeyword:
                    return "float";
                case SyntaxKind.DecimalKeyword:
                    return "decimal";
                case SyntaxKind.StringKeyword:
                    return "string";
                case SyntaxKind.CharKeyword:
                    return "char";
                case SyntaxKind.VoidKeyword:
                    return "void";
                case SyntaxKind.ObjectKeyword:
                    return "object";
                case SyntaxKind.TypeOfKeyword:
                    return "typeof";
                case SyntaxKind.SizeOfKeyword:
                    return "sizeof";
                case SyntaxKind.NullKeyword:
                    return "null";
                case SyntaxKind.TrueKeyword:
                    return "true";
                case SyntaxKind.FalseKeyword:
                    return "false";
                case SyntaxKind.IfKeyword:
                    return "if";
                case SyntaxKind.ElseKeyword:
                    return "else";
                case SyntaxKind.WhileKeyword:
                    return "while";
                case SyntaxKind.ForKeyword:
                    return "for";
                case SyntaxKind.ForEachKeyword:
                    return "foreach";
                case SyntaxKind.DoKeyword:
                    return "do";
                case SyntaxKind.SwitchKeyword:
                    return "switch";
                case SyntaxKind.CaseKeyword:
                    return "case";
                case SyntaxKind.DefaultKeyword:
                    return "default";
                case SyntaxKind.TryKeyword:
                    return "try";
                case SyntaxKind.CatchKeyword:
                    return "catch";
                case SyntaxKind.FinallyKeyword:
                    return "finally";
                case SyntaxKind.LockKeyword:
                    return "lock";
                case SyntaxKind.GotoKeyword:
                    return "goto";
                case SyntaxKind.BreakKeyword:
                    return "break";
                case SyntaxKind.ContinueKeyword:
                    return "continue";
                case SyntaxKind.ReturnKeyword:
                    return "return";
                case SyntaxKind.ThrowKeyword:
                    return "throw";
                case SyntaxKind.PublicKeyword:
                    return "public";
                case SyntaxKind.PrivateKeyword:
                    return "private";
                case SyntaxKind.InternalKeyword:
                    return "internal";
                case SyntaxKind.ProtectedKeyword:
                    return "protected";
                case SyntaxKind.StaticKeyword:
                    return "static";
                case SyntaxKind.ReadOnlyKeyword:
                    return "readonly";
                case SyntaxKind.SealedKeyword:
                    return "sealed";
                case SyntaxKind.ConstKeyword:
                    return "const";
                case SyntaxKind.FixedKeyword:
                    return "fixed";
                case SyntaxKind.StackAllocKeyword:
                    return "stackalloc";
                case SyntaxKind.VolatileKeyword:
                    return "volatile";
                case SyntaxKind.NewKeyword:
                    return "new";
                case SyntaxKind.OverrideKeyword:
                    return "override";
                case SyntaxKind.AbstractKeyword:
                    return "abstract";
                case SyntaxKind.VirtualKeyword:
                    return "virtual";
                case SyntaxKind.EventKeyword:
                    return "event";
                case SyntaxKind.ExternKeyword:
                    return "extern";
                case SyntaxKind.RefKeyword:
                    return "ref";
                case SyntaxKind.OutKeyword:
                    return "out";
                case SyntaxKind.InKeyword:
                    return "in";
                case SyntaxKind.IsKeyword:
                    return "is";
                case SyntaxKind.AsKeyword:
                    return "as";
                case SyntaxKind.ParamsKeyword:
                    return "params";
                case SyntaxKind.ArgListKeyword:
                    return "__arglist";
                case SyntaxKind.MakeRefKeyword:
                    return "__makeref";
                case SyntaxKind.RefTypeKeyword:
                    return "__reftype";
                case SyntaxKind.RefValueKeyword:
                    return "__refvalue";
                case SyntaxKind.ThisKeyword:
                    return "this";
                case SyntaxKind.BaseKeyword:
                    return "base";
                case SyntaxKind.NamespaceKeyword:
                    return "namespace";
                case SyntaxKind.UsingKeyword:
                    return "using";
                case SyntaxKind.ClassKeyword:
                    return "class";
                case SyntaxKind.StructKeyword:
                    return "struct";
                case SyntaxKind.InterfaceKeyword:
                    return "interface";
                case SyntaxKind.EnumKeyword:
                    return "enum";
                case SyntaxKind.DelegateKeyword:
                    return "delegate";
                case SyntaxKind.CheckedKeyword:
                    return "checked";
                case SyntaxKind.UncheckedKeyword:
                    return "unchecked";
                case SyntaxKind.UnsafeKeyword:
                    return "unsafe";
                case SyntaxKind.OperatorKeyword:
                    return "operator";
                case SyntaxKind.ImplicitKeyword:
                    return "implicit";
                case SyntaxKind.ExplicitKeyword:
                    return "explicit";
                case SyntaxKind.ElifKeyword:
                    return "elif";
                case SyntaxKind.EndIfKeyword:
                    return "endif";
                case SyntaxKind.RegionKeyword:
                    return "region";
                case SyntaxKind.EndRegionKeyword:
                    return "endregion";
                case SyntaxKind.DefineKeyword:
                    return "define";
                case SyntaxKind.UndefKeyword:
                    return "undef";
                case SyntaxKind.WarningKeyword:
                    return "warning";
                case SyntaxKind.ErrorKeyword:
                    return "error";
                case SyntaxKind.LineKeyword:
                    return "line";
                case SyntaxKind.PragmaKeyword:
                    return "pragma";
                case SyntaxKind.HiddenKeyword:
                    return "hidden";
                case SyntaxKind.ChecksumKeyword:
                    return "checksum";
                case SyntaxKind.DisableKeyword:
                    return "disable";
                case SyntaxKind.RestoreKeyword:
                    return "restore";
                case SyntaxKind.ReferenceKeyword:
                    return "r";
                case SyntaxKind.LoadKeyword:
                    return "load";
                case SyntaxKind.NullableKeyword:
                    return "nullable";
                case SyntaxKind.EnableKeyword:
                    return "enable";
                case SyntaxKind.WarningsKeyword:
                    return "warnings";
                case SyntaxKind.AnnotationsKeyword:
                    return "annotations";

                // contextual keywords
                case SyntaxKind.YieldKeyword:
                    return "yield";
                case SyntaxKind.PartialKeyword:
                    return "partial";
                case SyntaxKind.FromKeyword:
                    return "from";
                case SyntaxKind.GroupKeyword:
                    return "group";
                case SyntaxKind.JoinKeyword:
                    return "join";
                case SyntaxKind.IntoKeyword:
                    return "into";
                case SyntaxKind.LetKeyword:
                    return "let";
                case SyntaxKind.ByKeyword:
                    return "by";
                case SyntaxKind.WhereKeyword:
                    return "where";
                case SyntaxKind.SelectKeyword:
                    return "select";
                case SyntaxKind.GetKeyword:
                    return "get";
                case SyntaxKind.SetKeyword:
                    return "set";
                case SyntaxKind.AddKeyword:
                    return "add";
                case SyntaxKind.RemoveKeyword:
                    return "remove";
                case SyntaxKind.OrderByKeyword:
                    return "orderby";
                case SyntaxKind.AliasKeyword:
                    return "alias";
                case SyntaxKind.OnKeyword:
                    return "on";
                case SyntaxKind.EqualsKeyword:
                    return "equals";
                case SyntaxKind.AscendingKeyword:
                    return "ascending";
                case SyntaxKind.DescendingKeyword:
                    return "descending";
                case SyntaxKind.AssemblyKeyword:
                    return "assembly";
                case SyntaxKind.ModuleKeyword:
                    return "module";
                case SyntaxKind.TypeKeyword:
                    return "type";
                case SyntaxKind.FieldKeyword:
                    return "field";
                case SyntaxKind.MethodKeyword:
                    return "method";
                case SyntaxKind.ParamKeyword:
                    return "param";
                case SyntaxKind.PropertyKeyword:
                    return "property";
                case SyntaxKind.TypeVarKeyword:
                    return "typevar";
                case SyntaxKind.GlobalKeyword:
                    return "global";
                case SyntaxKind.NameOfKeyword:
                    return "nameof";
                case SyntaxKind.AsyncKeyword:
                    return "async";
                case SyntaxKind.AwaitKeyword:
                    return "await";
                case SyntaxKind.WhenKeyword:
                    return "when";
                case SyntaxKind.InterpolatedStringStartToken:
                    return "$\"";
                case SyntaxKind.InterpolatedStringEndToken:
                    return "\"";
                case SyntaxKind.InterpolatedVerbatimStringStartToken:
                    return "$@\"";
                case SyntaxKind.UnderscoreToken:
                    return "_";
                case SyntaxKind.VarKeyword:
                    return "var";
                default:
                    return string.Empty;
            }
        }

        public static bool IsTypeParameterVarianceKeyword(SyntaxKind kind)
        {
            return kind == SyntaxKind.OutKeyword || kind == SyntaxKind.InKeyword;
        }

        public static bool IsDocumentationCommentTrivia(SyntaxKind kind)
        {
            return kind == SyntaxKind.SingleLineDocumentationCommentTrivia ||
                kind == SyntaxKind.MultiLineDocumentationCommentTrivia;
        }
    }
}
