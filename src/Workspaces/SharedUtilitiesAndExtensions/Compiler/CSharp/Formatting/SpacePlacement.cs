// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [Flags]
    internal enum SpacePlacement
    {
        IgnoreAroundVariableDeclaration = 1,
        AfterMethodDeclarationName = 1 << 1,
        BetweenEmptyMethodDeclarationParentheses = 1 << 2,
        WithinMethodDeclarationParenthesis = 1 << 3,
        AfterMethodCallName = 1 << 4,
        BetweenEmptyMethodCallParentheses = 1 << 5,
        WithinMethodCallParentheses = 1 << 6,
        AfterControlFlowStatementKeyword = 1 << 7,
        WithinExpressionParentheses = 1 << 8,
        WithinCastParentheses = 1 << 9,
        BeforeSemicolonsInForStatement = 1 << 10,
        AfterSemicolonsInForStatement = 1 << 11,
        WithinOtherParentheses = 1 << 12,
        AfterCast = 1 << 13,
        BeforeOpenSquareBracket = 1 << 14,
        BetweenEmptySquareBrackets = 1 << 15,
        WithinSquareBrackets = 1 << 16,
        AfterColonInBaseTypeDeclaration = 1 << 17,
        BeforeColonInBaseTypeDeclaration = 1 << 18,
        AfterComma = 1 << 19,
        BeforeComma = 1 << 20,
        AfterDot = 1 << 21,
        BeforeDot = 1 << 22,
    }
}
