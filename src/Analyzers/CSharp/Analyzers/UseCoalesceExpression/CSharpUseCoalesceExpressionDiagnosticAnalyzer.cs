﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UseCoalesceExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseCoalesceExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseCoalesceExpressionDiagnosticAnalyzer :
        AbstractUseCoalesceExpressionDiagnosticAnalyzer<
            SyntaxKind,
            ExpressionSyntax,
            ConditionalExpressionSyntax,
            BinaryExpressionSyntax>
    {
        protected override ISyntaxFacts GetSyntaxFacts()
            => CSharpSyntaxFacts.Instance;
    }
}
