// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpConsumePreserveSigAnalyzer : ConsumePreserveSigAnalyzer<SyntaxKind>
    {
        protected override SyntaxKind InvocationExpressionSyntaxKind
        {
            get { return SyntaxKind.InvocationExpression; }
        }

        protected override bool IsExpressionStatementSyntaxKind(int rawKind)
        {
            return (SyntaxKind)rawKind == SyntaxKind.ExpressionStatement;
        }
    }
}
