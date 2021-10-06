// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    internal enum StackFrameKind
    {
        None = 0,
        EndOfLine,
        Text,
        MethodDeclaration,
        MemberAccess,
        Identifier,
        GenericTypeIdentifier,
        TypeArgument,
        TypeIdentifier,
        ParameterList,
        ArrayExpression,

        // Tokens 
        AmpersandToken,
        OpenBracketToken,
        CloseBracketToken,
        OpenParenToken,
        CloseParenToken,
        DotToken,
        TextToken,
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
        WhitespaceToken,

        // Trivia
        WhitespaceTrivia,
        AtTrivia, // "at " portion of the stack frame
        InTrivia, // optional " in " portion of the stack frame
        TrailingTrivia, // All trailing text that is not syntactically relavent
    }
}
