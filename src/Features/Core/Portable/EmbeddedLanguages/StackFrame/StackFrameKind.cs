// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;

internal enum StackFrameKind
{
    None = 0,

    // Nodes
    CompilationUnit,
    MethodDeclaration,
    MemberAccess,
    ArrayTypeExpression,
    GenericTypeIdentifier,
    GeneratedIdentifier,
    LocalMethodIdentifier,
    StateMachineMethodIdentifier,
    TypeArgument,
    TypeIdentifier,
    Parameter,
    ParameterList,
    ArrayExpression,
    FileInformation,
    Constructor,

    // Tokens 
    EndOfFrame,
    AmpersandToken,
    OpenBracketToken,
    CloseBracketToken,
    OpenParenToken,
    CloseParenToken,
    DotToken,
    PlusToken,
    CommaToken,
    ColonToken,
    EqualsToken,
    GreaterThanToken,
    LessThanToken,
    MinusToken,
    SingleQuoteToken,
    GraveAccentToken, // `
    BackslashToken,
    ForwardSlashToken,
    IdentifierToken,
    PathToken,
    NumberToken,
    DollarToken,
    PipeToken,
    GeneratedNameSeparatorToken, // {character}__{identifier}
    GeneratedNameSuffixToken, // {numeric}_{numeric}
    ConstructorToken, // .ctor

    // Trivia
    WhitespaceTrivia,
    AtTrivia, // "at " portion of the stack frame
    InTrivia, // optional " in " portion of the stack frame
    LineTrivia, // optional "line " string indicating the line number of a file
    SkippedTextTrivia, // any skipped text that isn't a node, token, or special kind of trivia already presented
}
