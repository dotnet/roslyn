// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                case PrecedenceKind.Arithmetic: return CodeStyleOptions.ArithmeticOperationParentheses;
                case PrecedenceKind.Shift: return CodeStyleOptions.ShiftOperationParentheses;
                case PrecedenceKind.Relational: return CodeStyleOptions.RelationalOperationParentheses;
                case PrecedenceKind.Equality: return CodeStyleOptions.EqualityOperationParentheses;
                case PrecedenceKind.Bitwise: return CodeStyleOptions.BitwiseOperationParentheses;
                case PrecedenceKind.Logical: return CodeStyleOptions.LogicalOperationParentheses;
                case PrecedenceKind.Coalesce: return CodeStyleOptions.CoalesceOperationParentheses;
                case PrecedenceKind.Other: return CodeStyleOptions.OtherOperationParentheses;
            }

            throw ExceptionUtilities.UnexpectedValue(precedenceKind);
        }
    }
}
