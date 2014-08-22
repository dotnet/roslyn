// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Design
{
    /// <summary>
    /// Implements CA1027 and CA2217
    /// 
    /// 1) CA1027: Mark enums with FlagsAttribute
    /// 
    /// Cause:
    /// The values of a public enumeration are powers of two or are combinations of other values that are defined in the enumeration,
    /// and the System.FlagsAttribute attribute is not present.
    /// To reduce false positives, this rule does not report a violation for enumerations that have contiguous values.
    /// 
    /// 2) CA2217: Do not mark enums with FlagsAttribute
    /// 
    /// Cause:
    /// An externally visible enumeration is marked with FlagsAttribute and it has one or more values that are not powers of two or
    /// a combination of the other defined values on the enumeration.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpEnumWithFlagsDiagnosticAnalyzer : EnumWithFlagsDiagnosticAnalyzer
    {
        protected override Location GetDiagnosticLocation(SyntaxNode type)
        {
            return ((EnumDeclarationSyntax)type).Identifier.GetLocation();
        }
    }
}
