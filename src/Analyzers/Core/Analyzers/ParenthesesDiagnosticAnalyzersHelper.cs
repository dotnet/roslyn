// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Precedence;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

internal static class ParenthesesDiagnosticAnalyzersHelper
{
    internal static ImmutableHashSet<IOption2> Options =
    [
        CodeStyleOptions2.ArithmeticBinaryParentheses,
        CodeStyleOptions2.RelationalBinaryParentheses,
        CodeStyleOptions2.OtherBinaryParentheses,
        CodeStyleOptions2.OtherParentheses,
    ];

    internal static CodeStyleOption2<ParenthesesPreference> GetLanguageOption(AnalyzerOptionsProvider options, PrecedenceKind precedenceKind)
        => precedenceKind switch
        {
            PrecedenceKind.Arithmetic or PrecedenceKind.Shift or PrecedenceKind.Bitwise => options.ArithmeticBinaryParentheses,
            PrecedenceKind.Relational or PrecedenceKind.Equality => options.RelationalBinaryParentheses,
            PrecedenceKind.Logical or PrecedenceKind.Coalesce => options.OtherBinaryParentheses,
            PrecedenceKind.Other => options.OtherParentheses,
            _ => throw ExceptionUtilities.UnexpectedValue(precedenceKind),
        };
}
