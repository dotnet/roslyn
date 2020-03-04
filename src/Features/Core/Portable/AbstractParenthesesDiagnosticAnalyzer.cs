﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
{
    internal abstract class AbstractParenthesesDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        protected AbstractParenthesesDiagnosticAnalyzer(
            string descriptorId, LocalizableString title, LocalizableString message)
            : base(descriptorId,
                   options: ImmutableHashSet.Create<IPerLanguageOption>(CodeStyleOptions.ArithmeticBinaryParentheses, CodeStyleOptions.RelationalBinaryParentheses, CodeStyleOptions.OtherBinaryParentheses, CodeStyleOptions.OtherParentheses),
                   title, message)
        {
        }

        protected AbstractParenthesesDiagnosticAnalyzer(ImmutableArray<DiagnosticDescriptor> diagnosticDescriptors)
            : base(diagnosticDescriptors)
        {
        }

        protected PerLanguageOption<CodeStyleOption<ParenthesesPreference>> GetLanguageOption(PrecedenceKind precedenceKind)
        {
            switch (precedenceKind)
            {
                case PrecedenceKind.Arithmetic:
                case PrecedenceKind.Shift:
                case PrecedenceKind.Bitwise:
                    return CodeStyleOptions.ArithmeticBinaryParentheses;
                case PrecedenceKind.Relational:
                case PrecedenceKind.Equality:
                    return CodeStyleOptions.RelationalBinaryParentheses;
                case PrecedenceKind.Logical:
                case PrecedenceKind.Coalesce:
                    return CodeStyleOptions.OtherBinaryParentheses;
                case PrecedenceKind.Other:
                    return CodeStyleOptions.OtherParentheses;
            }

            throw ExceptionUtilities.UnexpectedValue(precedenceKind);
        }
    }
}
