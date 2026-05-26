// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal enum SyntaxKind : byte
{
    None,

    #region Nodes
    // Common
    RazorDocument,
    GenericBlock,
    RazorComment,
    RazorMetaCode,
    RazorDirective,
    RazorUsingDirective,
    RazorDirectiveBody,
    UnclassifiedTextLiteral,

    // Markup
    MarkupBlock,
    MarkupTransition,
    MarkupElement,
    MarkupStartTag,
    MarkupEndTag,
    MarkupTagBlock,
    MarkupTextLiteral,
    MarkupEphemeralTextLiteral,
    MarkupCommentBlock,
    MarkupAttributeBlock,
    MarkupMinimizedAttributeBlock,
    MarkupMiscAttributeContent,
    MarkupLiteralAttributeValue,
    MarkupDynamicAttributeValue,
    MarkupTagHelperElement,
    MarkupTagHelperStartTag,
    MarkupTagHelperEndTag,
    MarkupTagHelperAttribute,
    MarkupMinimizedTagHelperAttribute,
    MarkupTagHelperDirectiveAttribute,
    MarkupMinimizedTagHelperDirectiveAttribute,
    MarkupTagHelperAttributeValue,

    // CSharp
    CSharpStatement,
    CSharpStatementBody,
    CSharpExplicitExpression,
    CSharpExplicitExpressionBody,
    CSharpImplicitExpression,
    CSharpImplicitExpressionBody,
    CSharpCodeBlock,
    CSharpTemplateBlock,
    CSharpStatementLiteral,
    CSharpExpressionLiteral,
    CSharpEphemeralTextLiteral,
    CSharpTransition,
    #endregion

    #region Tokens
    // Common
    Marker,
    List,
    Whitespace,
    NewLine,
    Colon,
    QuestionMark,
    RightBracket,
    LeftBracket,
    Equals,
    Transition,

    // HTML
    Text,
    OpenAngle,
    Bang,
    ForwardSlash,
    DoubleHyphen,
    CloseAngle,
    DoubleQuote,
    SingleQuote,

    // CSharp literals
    Identifier,
    Keyword,
    IntegerLiteral,
    NumericLiteral,
    CSharpComment,
    RealLiteral,
    CharacterLiteral,
    StringLiteral,
    CSharpDirective,
    CSharpDisabledText,

    // CSharp operators
    CSharpOperator,

    Arrow,
    Minus,
    Decrement,
    MinusAssign,
    NotEqual,
    Not,
    Modulo,
    ModuloAssign,
    AndAssign,
    And,
    DoubleAnd,
    LeftParenthesis,
    RightParenthesis,
    Star,
    MultiplyAssign,
    Comma,
    Dot,
    Slash,
    DivideAssign,
    DoubleColon,
    Semicolon,
    NullCoalesce,
    XorAssign,
    Xor,
    LeftBrace,
    OrAssign,
    DoubleOr,
    Or,
    RightBrace,
    Tilde,
    Plus,
    PlusAssign,
    Increment,
    LessThan,
    LessThanEqual,
    LeftShift,
    LeftShiftAssign,
    Assign,
    GreaterThan,
    GreaterThanEqual,
    RightShift,
    RightShiftAssign,
    Hash,

    // Razor specific
    RazorCommentLiteral,
    RazorCommentStar,
    RazorCommentTransition,

    // New common (Consider condensing when https://github.com/dotnet/razor/issues/8400 is done and we can break the API).
    EndOfFile,
    ConflictMarkerTrivia,
    #endregion

    // New nodes should go before this one

    FirstAvailableTokenKind,
}
