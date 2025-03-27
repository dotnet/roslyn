// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.ExpressionEvaluator;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
{
    internal sealed partial class MemberSignatureParser
    {
        internal static readonly StringComparer StringComparer = StringComparer.OrdinalIgnoreCase;
        internal static readonly ImmutableHashSet<string> Keywords = GetKeywords(StringComparer);
        internal static readonly ImmutableDictionary<string, SyntaxKind> KeywordKinds = GetKeywordKinds(StringComparer);

        internal static RequestSignature Parse(string signature)
        {
            var scanner = new Scanner(signature);
            var builder = ImmutableArray.CreateBuilder<Token>();
            Token token;
            do
            {
                scanner.MoveNext();
                token = scanner.CurrentToken;
                builder.Add(token);
            } while (token.Kind != TokenKind.End);
            var parser = new MemberSignatureParser(builder.ToImmutable());
            try
            {
                return parser.Parse();
            }
            catch (InvalidSignatureException)
            {
                return null;
            }
        }

        private readonly ImmutableArray<Token> _tokens;
        private int _tokenIndex;

        private MemberSignatureParser(ImmutableArray<Token> tokens)
        {
            _tokens = tokens;
            _tokenIndex = 0;
        }

        private Token CurrentToken => _tokens[_tokenIndex];

        private Token PeekToken(int offset)
        {
            return _tokens[_tokenIndex + offset];
        }

        private Token EatToken()
        {
            var token = CurrentToken;
            Debug.Assert(token.Kind != TokenKind.End);
            _tokenIndex++;
            return token;
        }

        private RequestSignature Parse()
        {
            var methodName = ParseName();
            var parameters = default(ImmutableArray<ParameterSignature>);
            if (CurrentToken.Kind == TokenKind.OpenParen)
            {
                parameters = ParseParameters();
            }
            if (CurrentToken.Kind != TokenKind.End)
            {
                throw InvalidSignature();
            }
            return new RequestSignature(methodName, parameters);
        }

        private Name ParseName()
        {
            Name signature = null;
            while (true)
            {
                switch (CurrentToken.Kind)
                {
                    case TokenKind.Identifier:
                        break;
                    case TokenKind.Keyword:
                        if (signature == null)
                        {
                            throw InvalidSignature();
                        }
                        break;
                    default:
                        throw InvalidSignature();
                }
                var name = EatToken().Text;
                signature = new QualifiedName(signature, name);
                if (IsStartOfTypeArguments())
                {
                    var typeParameters = ParseTypeParameters();
                    signature = new GenericName((QualifiedName)signature, typeParameters);
                }
                if (CurrentToken.Kind != TokenKind.Dot)
                {
                    return signature;
                }
                EatToken();
            }
        }

        private ImmutableArray<string> ParseTypeParameters()
        {
            Debug.Assert(IsStartOfTypeArguments());
            EatToken();
            EatToken();
            var builder = ImmutableArray.CreateBuilder<string>();
            while (true)
            {
                if (CurrentToken.Kind != TokenKind.Identifier)
                {
                    throw InvalidSignature();
                }
                var name = EatToken().Text;
                builder.Add(name);
                switch (CurrentToken.Kind)
                {
                    case TokenKind.CloseParen:
                        EatToken();
                        return builder.ToImmutable();
                    case TokenKind.Comma:
                        EatToken();
                        break;
                    default:
                        throw InvalidSignature();
                }
            }
        }

        private TypeSignature ParseTypeName()
        {
            TypeSignature signature = null;
            while (true)
            {
                switch (CurrentToken.Kind)
                {
                    case TokenKind.Identifier:
                        {
                            var token = EatToken();
                            var name = token.Text;
                            signature = new QualifiedTypeSignature(signature, name);
                        }
                        break;
                    case TokenKind.Keyword:
                        if (signature == null)
                        {
                            // Expand special type keywords (Object, Integer, etc.) to qualified names.
                            // This is only done for the first identifier in a qualified name.
                            var specialType = GetSpecialType(CurrentToken.KeywordKind);
                            if (specialType != SpecialType.None)
                            {
                                EatToken();
                                signature = specialType.GetTypeSignature();
                                Debug.Assert(signature != null);
                            }
                            if (signature == null)
                            {
                                throw InvalidSignature();
                            }
                        }
                        else
                        {
                            var token = EatToken();
                            var name = token.Text;
                            signature = new QualifiedTypeSignature(signature, name);
                        }
                        break;
                    default:
                        throw InvalidSignature();
                }
                if (IsStartOfTypeArguments())
                {
                    var typeArguments = ParseTypeArguments();
                    signature = new GenericTypeSignature((QualifiedTypeSignature)signature, typeArguments);
                }
                if (CurrentToken.Kind != TokenKind.Dot)
                {
                    return signature;
                }
                EatToken();
            }
        }

        private ImmutableArray<TypeSignature> ParseTypeArguments()
        {
            Debug.Assert(IsStartOfTypeArguments());
            EatToken();
            EatToken();
            var builder = ImmutableArray.CreateBuilder<TypeSignature>();
            while (true)
            {
                var name = ParseType();
                builder.Add(name);
                switch (CurrentToken.Kind)
                {
                    case TokenKind.CloseParen:
                        EatToken();
                        return builder.ToImmutable();
                    case TokenKind.Comma:
                        EatToken();
                        break;
                    default:
                        throw InvalidSignature();
                }
            }
        }

        private TypeSignature ParseType()
        {
            TypeSignature type = ParseTypeName();
            while (true)
            {
                switch (CurrentToken.Kind)
                {
                    case TokenKind.OpenParen:
                        EatToken();
                        int rank = 1;
                        while (CurrentToken.Kind == TokenKind.Comma)
                        {
                            EatToken();
                            rank++;
                        }
                        if (CurrentToken.Kind != TokenKind.CloseParen)
                        {
                            throw InvalidSignature();
                        }
                        EatToken();
                        type = new ArrayTypeSignature(type, rank);
                        break;
                    case TokenKind.QuestionMark:
                        EatToken();
                        type = new GenericTypeSignature(
                            SpecialType.System_Nullable_T.GetTypeSignature(),
                            ImmutableArray.Create(type));
                        break;
                    default:
                        return type;
                }
            }
        }

        private bool IsStartOfTypeArguments()
        {
            return CurrentToken.Kind == TokenKind.OpenParen &&
                PeekToken(1).KeywordKind == SyntaxKind.OfKeyword;
        }

        private enum ParameterModifier
        {
            None,
            ByVal,
            ByRef,
        }

        private ParameterModifier ParseParameterModifier()
        {
            var modifier = ParameterModifier.None;
            while (true)
            {
                var m = ParameterModifier.None;
                switch (CurrentToken.KeywordKind)
                {
                    case SyntaxKind.ByValKeyword:
                        m = ParameterModifier.ByVal;
                        break;
                    case SyntaxKind.ByRefKeyword:
                        m = ParameterModifier.ByRef;
                        break;
                    default:
                        return modifier;
                }
                if (modifier != ParameterModifier.None)
                {
                    // Duplicate modifiers.
                    throw InvalidSignature();
                }
                modifier = m;
                EatToken();
            }
        }

        private ImmutableArray<ParameterSignature> ParseParameters()
        {
            Debug.Assert(CurrentToken.Kind == TokenKind.OpenParen);
            EatToken();
            if (CurrentToken.Kind == TokenKind.CloseParen)
            {
                EatToken();
                return [];
            }
            var builder = ImmutableArray.CreateBuilder<ParameterSignature>();
            while (true)
            {
                var modifier = ParseParameterModifier();
                var parameterType = ParseType();
                builder.Add(new ParameterSignature(parameterType, isByRef: modifier == ParameterModifier.ByRef));
                switch (CurrentToken.Kind)
                {
                    case TokenKind.CloseParen:
                        EatToken();
                        return builder.ToImmutable();
                    case TokenKind.Comma:
                        EatToken();
                        break;
                    default:
                        throw InvalidSignature();
                }
            }
        }

        private static SpecialType GetSpecialType(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.BooleanKeyword:
                    return SpecialType.System_Boolean;
                case SyntaxKind.CharKeyword:
                    return SpecialType.System_Char;
                case SyntaxKind.SByteKeyword:
                    return SpecialType.System_SByte;
                case SyntaxKind.ByteKeyword:
                    return SpecialType.System_Byte;
                case SyntaxKind.ShortKeyword:
                    return SpecialType.System_Int16;
                case SyntaxKind.UShortKeyword:
                    return SpecialType.System_UInt16;
                case SyntaxKind.IntegerKeyword:
                    return SpecialType.System_Int32;
                case SyntaxKind.UIntegerKeyword:
                    return SpecialType.System_UInt32;
                case SyntaxKind.LongKeyword:
                    return SpecialType.System_Int64;
                case SyntaxKind.ULongKeyword:
                    return SpecialType.System_UInt64;
                case SyntaxKind.SingleKeyword:
                    return SpecialType.System_Single;
                case SyntaxKind.DoubleKeyword:
                    return SpecialType.System_Double;
                case SyntaxKind.StringKeyword:
                    return SpecialType.System_String;
                case SyntaxKind.ObjectKeyword:
                    return SpecialType.System_Object;
                case SyntaxKind.DecimalKeyword:
                    return SpecialType.System_Decimal;
                case SyntaxKind.DateKeyword:
                    return SpecialType.System_DateTime;
                default:
                    return SpecialType.None;
            }
        }

        private static Exception InvalidSignature()
        {
            return new InvalidSignatureException();
        }

        private sealed class InvalidSignatureException : Exception
        {
        }
    }
}
