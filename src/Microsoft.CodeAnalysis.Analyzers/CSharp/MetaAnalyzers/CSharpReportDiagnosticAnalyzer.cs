// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpReportDiagnosticAnalyzer : ReportDiagnosticAnalyzer<ClassDeclarationSyntax, InvocationExpressionSyntax, IdentifierNameSyntax, VariableDeclaratorSyntax>
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

            protected override IEnumerable<SyntaxNode>? GetArgumentExpressions(InvocationExpressionSyntax invocation)
            {
                if (invocation.ArgumentList != null)
                {
                    return invocation.ArgumentList.Arguments.Select(a => a.Expression);
                }

                return null;
            }

            protected override SyntaxNode GetPropertyGetterBlockSyntax(SyntaxNode declaringSyntaxRefNode)
            {
                if (declaringSyntaxRefNode is AccessorDeclarationSyntax accessor &&
                    accessor.Body == null &&
                    accessor.ExpressionBody == null)
                {
                    // Walk up to the property initializer.
                    var propertyDecl = accessor.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
                    if (propertyDecl?.Initializer != null)
                    {
                        return propertyDecl.Initializer;
                    }
                }

                return base.GetPropertyGetterBlockSyntax(declaringSyntaxRefNode);
            }
        }
    }
}
