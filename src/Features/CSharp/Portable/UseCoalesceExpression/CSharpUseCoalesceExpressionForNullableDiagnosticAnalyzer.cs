// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UseCoalesceExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseCoalesceExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseCoalesceExpressionForNullableDiagnosticAnalyzer :
        AbstractUseCoalesceExpressionForNullableDiagnosticAnalyzer<
            SyntaxKind,
            ExpressionSyntax,
            ConditionalExpressionSyntax,
            BinaryExpressionSyntax,
            MemberAccessExpressionSyntax,
            PrefixUnaryExpressionSyntax>
    {
        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override SyntaxKind GetSyntaxKindToAnalyze()
            => SyntaxKind.ConditionalExpression;
    }
}
