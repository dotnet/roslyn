// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.MetaAnalyzers.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpReportDiagnosticAnalyzer : ReportDiagnosticAnalyzer<ClassDeclarationSyntax, InvocationExpressionSyntax, IdentifierNameSyntax>
    {
        protected override ReportDiagnosticCompilationAnalyzer GetAnalyzer(ImmutableHashSet<INamedTypeSymbol> contextTypes, INamedTypeSymbol diagnosticType, INamedTypeSymbol diagnosticDescriptorType, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            return new CSharpReportDiagnosticCompilationAnalyzer(contextTypes, diagnosticType, diagnosticDescriptorType, diagnosticAnalyzer, diagnosticAnalyzerAttribute);
        }

        private sealed class CSharpReportDiagnosticCompilationAnalyzer : ReportDiagnosticCompilationAnalyzer
        {
            public CSharpReportDiagnosticCompilationAnalyzer(ImmutableHashSet<INamedTypeSymbol> contextTypes, INamedTypeSymbol diagnosticType, INamedTypeSymbol diagnosticDescriptorType, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
                : base(contextTypes, diagnosticType, diagnosticDescriptorType, diagnosticAnalyzer, diagnosticAnalyzerAttribute)
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
