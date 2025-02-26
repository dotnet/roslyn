// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpReportDiagnosticAnalyzer : ReportDiagnosticAnalyzer<ClassDeclarationSyntax, StructDeclarationSyntax, InvocationExpressionSyntax, IdentifierNameSyntax, VariableDeclaratorSyntax>
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
