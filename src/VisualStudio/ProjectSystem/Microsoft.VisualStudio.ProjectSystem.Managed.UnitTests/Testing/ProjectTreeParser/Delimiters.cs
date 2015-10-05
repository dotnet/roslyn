// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using static Microsoft.VisualStudio.Testing.Tokenizer;

namespace Microsoft.VisualStudio.Testing
{
    internal static class Delimiters
    {
        public static readonly ImmutableArray<TokenType> QuotedPropertyValue = ImmutableArray.Create(TokenType.Quote, TokenType.CarriageReturn, TokenType.NewLine);
        public static readonly ImmutableArray<TokenType> BracedPropertyValueBlock = ImmutableArray.Create(TokenType.RightBrace, TokenType.LeftBrace, TokenType.WhiteSpace, TokenType.CarriageReturn, TokenType.NewLine);
        public static readonly ImmutableArray<TokenType> BracedPropertyValue = ImmutableArray.Create(TokenType.RightBrace, TokenType.WhiteSpace, TokenType.CarriageReturn, TokenType.NewLine);
        public static readonly ImmutableArray<TokenType> Caption = ImmutableArray.Create(TokenType.LeftParenthesis, TokenType.Comma, TokenType.CarriageReturn, TokenType.NewLine);
        public static readonly ImmutableArray<TokenType> PropertyName = ImmutableArray.Create(TokenType.Colon, TokenType.CarriageReturn, TokenType.NewLine, TokenType.WhiteSpace);
        public static readonly ImmutableArray<TokenType> PropertyValue = ImmutableArray.Create(TokenType.WhiteSpace, TokenType.Comma, TokenType.RightParenthesis, TokenType.CarriageReturn, TokenType.NewLine);
        public static readonly ImmutableArray<TokenType> Structural = ImmutableArray.Create(TokenType.Comma, TokenType.LeftParenthesis, TokenType.RightParenthesis, TokenType.WhiteSpace, TokenType.NewLine, TokenType.CarriageReturn);
    }
}
