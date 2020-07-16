// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Precedence;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
{
    internal abstract class AbstractParenthesesDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        protected AbstractParenthesesDiagnosticAnalyzer(
            string descriptorId, LocalizableString title, LocalizableString message)
            : base(descriptorId,
                   options: ImmutableHashSet.Create<IPerLanguageOption>(CodeStyleOptions2.ArithmeticBinaryParentheses, CodeStyleOptions2.RelationalBinaryParentheses, CodeStyleOptions2.OtherBinaryParentheses, CodeStyleOptions2.OtherParentheses),
                   title, message)
        {
        }

        protected AbstractParenthesesDiagnosticAnalyzer(ImmutableArray<DiagnosticDescriptor> diagnosticDescriptors)
            : base(diagnosticDescriptors)
        {
        }

        protected static PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> GetLanguageOption(PrecedenceKind precedenceKind)
        {
            switch (precedenceKind)
            {
                case PrecedenceKind.Arithmetic:
                case PrecedenceKind.Shift:
                case PrecedenceKind.Bitwise:
                    return CodeStyleOptions2.ArithmeticBinaryParentheses;
                case PrecedenceKind.Relational:
                case PrecedenceKind.Equality:
                    return CodeStyleOptions2.RelationalBinaryParentheses;
                case PrecedenceKind.Logical:
                case PrecedenceKind.Coalesce:
                    return CodeStyleOptions2.OtherBinaryParentheses;
                case PrecedenceKind.Other:
                    return CodeStyleOptions2.OtherParentheses;
            }

            throw ExceptionUtilities.UnexpectedValue(precedenceKind);
        }
    }
}
