// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Precedence;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
{
    internal abstract class AbstractParenthesesDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        protected AbstractParenthesesDiagnosticAnalyzer(
            string descriptorId, EnforceOnBuild enforceOnBuild, LocalizableString title, LocalizableString message, bool isUnnecessary = false)
            : base(descriptorId,
                   enforceOnBuild,
                   options: ImmutableHashSet.Create<IPerLanguageOption>(CodeStyleOptions2.ArithmeticBinaryParentheses, CodeStyleOptions2.RelationalBinaryParentheses, CodeStyleOptions2.OtherBinaryParentheses, CodeStyleOptions2.OtherParentheses),
                   title,
                   message,
                   isUnnecessary: isUnnecessary)
        {
        }

        protected static CodeStyleOption2<ParenthesesPreference> GetLanguageOption(AnalyzerOptionsProvider options, PrecedenceKind precedenceKind)
            => precedenceKind switch
            {
                PrecedenceKind.Arithmetic or PrecedenceKind.Shift or PrecedenceKind.Bitwise => options.ArithmeticBinaryParentheses,
                PrecedenceKind.Relational or PrecedenceKind.Equality => options.RelationalBinaryParentheses,
                PrecedenceKind.Logical or PrecedenceKind.Coalesce => options.OtherBinaryParentheses,
                PrecedenceKind.Other => options.OtherParentheses,
                _ => throw ExceptionUtilities.UnexpectedValue(precedenceKind),
            };

        protected static string GetEquivalenceKey(PrecedenceKind precedenceKind)
            => precedenceKind switch
            {
                PrecedenceKind.Arithmetic or PrecedenceKind.Shift or PrecedenceKind.Bitwise => "ArithmeticBinary",
                PrecedenceKind.Relational or PrecedenceKind.Equality => "RelationalBinary",
                PrecedenceKind.Logical or PrecedenceKind.Coalesce => "OtherBinary",
                PrecedenceKind.Other => "Other",
                _ => throw ExceptionUtilities.UnexpectedValue(precedenceKind),
            };

        protected static ImmutableArray<string> GetAllEquivalenceKeys()
            => ImmutableArray.Create("ArithmeticBinary", "RelationalBinary", "OtherBinary", "Other");
    }
}
