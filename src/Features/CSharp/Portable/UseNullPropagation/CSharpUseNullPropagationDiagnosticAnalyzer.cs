// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UseNullPropagation;

namespace Microsoft.CodeAnalysis.CSharp.UseNullPropagation
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseNullPropagationDiagnosticAnalyzer :
        AbstractUseNullPropagationDiagnosticAnalyzer<
            SyntaxKind,
            ExpressionSyntax,
            ConditionalExpressionSyntax,
            BinaryExpressionSyntax,
            InvocationExpressionSyntax,
            MemberAccessExpressionSyntax,
            ConditionalAccessExpressionSyntax,
            ElementAccessExpressionSyntax>
    {
        protected override bool ShouldAnalyze(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp6;

        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override ISemanticFactsService GetSemanticFactsService()
            => CSharpSemanticFactsService.Instance;

        protected override SyntaxKind GetSyntaxKindToAnalyze()
            => SyntaxKind.ConditionalExpression;

        protected override bool IsEquals(BinaryExpressionSyntax condition)
            => condition.Kind() == SyntaxKind.EqualsExpression;

        protected override bool IsNotEquals(BinaryExpressionSyntax condition)
            => condition.Kind() == SyntaxKind.NotEqualsExpression;
    }
}
