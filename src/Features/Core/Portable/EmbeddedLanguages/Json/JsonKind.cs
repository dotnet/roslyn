// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json;

internal enum JsonKind
{
    None = 0,

    // Nodes
    CompilationUnit,
    Text,
    Object,
    Array,
    Literal,
    // Used to represent `-Infinity` which is supported by Json.Net
    NegativeLiteral,
    Property,
    Constructor,
    CommaValue,

    // Tokens
    EndOfFile,
    OpenBraceToken,
    CloseBraceToken,
    OpenBracketToken,
    CloseBracketToken,
    OpenParenToken,
    CloseParenToken,
    StringToken,
    NumberToken,
    TextToken,
    ColonToken,
    CommaToken,
    TrueLiteralToken,
    FalseLiteralToken,
    NullLiteralToken,
    UndefinedLiteralToken,
    NaNLiteralToken,
    InfinityLiteralToken,
    NegativeInfinityLiteralToken,
    MinusToken,
    NewKeyword,

    // Trivia
    SingleLineCommentTrivia,
    MultiLineCommentTrivia,
    WhitespaceTrivia,
    EndOfLineTrivia,
}
