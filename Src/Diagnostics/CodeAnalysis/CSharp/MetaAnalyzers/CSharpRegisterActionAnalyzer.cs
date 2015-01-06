// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpRegisterActionAnalyzer : RegisterActionAnalyzer<ClassDeclarationSyntax, InvocationExpressionSyntax, SyntaxKind>
    {
        protected override RegisterActionCompilationAnalyzer GetAnalyzer(INamedTypeSymbol analysisContext, INamedTypeSymbol compilationStartAnalysisContext, INamedTypeSymbol codeBlockStartAnalysisContext, INamedTypeSymbol symbolKind, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            return new CSharpRegisterActionCompilationAnalyzer(analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext, symbolKind, diagnosticAnalyzer, diagnosticAnalyzerAttribute);
        }

        private sealed class CSharpRegisterActionCompilationAnalyzer : RegisterActionCompilationAnalyzer
        {
            public CSharpRegisterActionCompilationAnalyzer(INamedTypeSymbol analysisContext, INamedTypeSymbol compilationStartAnalysisContext, INamedTypeSymbol codeBlockStartAnalysisContext, INamedTypeSymbol symbolKind, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
                : base(analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext, symbolKind, diagnosticAnalyzer, diagnosticAnalyzerAttribute)
            {
            }

            protected override IEnumerable<SyntaxNode> GetArgumentExpressions(InvocationExpressionSyntax invocation)
            {
                if (invocation.ArgumentList != null)
                {
                    return invocation.ArgumentList.Arguments.Select(a => a.Expression);
                }

                return null;
            }
        }
    }
}
