// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public abstract partial class DiagnosticAnalyzerCorrectnessAnalyzer : DiagnosticAnalyzer
    {
        protected abstract class DiagnosticAnalyzerSymbolAnalyzer
        {
            protected DiagnosticAnalyzerSymbolAnalyzer(INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
            {
                DiagnosticAnalyzer = diagnosticAnalyzer;
                DiagnosticAnalyzerAttribute = diagnosticAnalyzerAttribute;
            }

            protected INamedTypeSymbol DiagnosticAnalyzer { get; }
            protected INamedTypeSymbol DiagnosticAnalyzerAttribute { get; }

            protected bool IsDiagnosticAnalyzer(INamedTypeSymbol type)
            {
                return SymbolEqualityComparer.Default.Equals(type, DiagnosticAnalyzer);
            }

            internal void AnalyzeSymbol(SymbolAnalysisContext symbolContext)
            {
                var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                if (namedType.GetBaseTypes().Any(IsDiagnosticAnalyzer))
                {
                    AnalyzeDiagnosticAnalyzer(symbolContext);
                }
            }

            protected abstract void AnalyzeDiagnosticAnalyzer(SymbolAnalysisContext symbolContext);

            protected bool HasDiagnosticAnalyzerAttribute(INamedTypeSymbol namedType, INamedTypeSymbol? attributeUsageAttribute)
            {
                foreach (AttributeData attribute in namedType.GetApplicableAttributes(attributeUsageAttribute))
                {
                    if (attribute.AttributeClass.DerivesFrom(DiagnosticAnalyzerAttribute))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
