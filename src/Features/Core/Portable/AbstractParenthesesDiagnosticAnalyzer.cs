﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
{
    internal abstract class AbstractParenthesesDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        protected AbstractParenthesesDiagnosticAnalyzer(
            string descriptorId, LocalizableString title, LocalizableString message)
            : base(descriptorId, title, message)
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
