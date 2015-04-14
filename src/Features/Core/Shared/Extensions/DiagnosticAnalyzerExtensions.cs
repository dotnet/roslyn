// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.EngineV1;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class DiagnosticAnalyzerExtensions
    {
        public static async Task<DiagnosticAnalyzerCategory> GetDiagnosticAnalyzerCategoryAsync(this DiagnosticAnalyzer analyzer, DiagnosticAnalyzerDriver driver)
        {
            var category = DiagnosticAnalyzerCategory.None;

            if (analyzer is DocumentDiagnosticAnalyzer)
            {
                category |= DiagnosticAnalyzerCategory.SyntaxAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
            }
            else if (analyzer is ProjectDiagnosticAnalyzer)
            {
                category |= DiagnosticAnalyzerCategory.ProjectAnalysis;
            }
            else if (driver != null)
            {
                // If an analyzer requires or might require the entire document, then it cannot promise
                // to be able to operate on a limited span of the document. In practical terms, no analyzer
                // can have both SemanticDocumentAnalysis and SemanticSpanAnalysis as categories.
                bool cantSupportSemanticSpanAnalysis = false;
                var analyzerActions = await driver.GetAnalyzerActionsAsync(analyzer).ConfigureAwait(false);
                if (analyzerActions != null)
                {
                    if (analyzerActions.SyntaxTreeActionsCount > 0)
                    {
                        category |= DiagnosticAnalyzerCategory.SyntaxAnalysis;
                    }

                    if (analyzerActions.SemanticModelActionsCount > 0)
                    {
                        category |= DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
                        cantSupportSemanticSpanAnalysis = true;
                    }

                    if (analyzerActions.CompilationStartActionsCount > 0)
                    {
                        // It is not possible to know what actions a compilation start action will register without executing it,
                        // so return a worst-case categorization.
                        category |= (DiagnosticAnalyzerCategory.SyntaxAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis | DiagnosticAnalyzerCategory.ProjectAnalysis);
                        cantSupportSemanticSpanAnalysis = true;
                    }

                    if (analyzerActions.CompilationActionsCount > 0 || analyzerActions.CompilationStartActionsCount > 0)
                    {
                        category |= DiagnosticAnalyzerCategory.ProjectAnalysis;
                    }

                    if (HasSemanticDocumentActions(analyzerActions))
                    {
                        var semanticDocumentAnalysisCategory = cantSupportSemanticSpanAnalysis ?
                            DiagnosticAnalyzerCategory.SemanticDocumentAnalysis :
                            DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
                        category |= semanticDocumentAnalysisCategory;
                    }
                }
            }

            return category;
        }

        private static bool HasSemanticDocumentActions(AnalyzerActions analyzerActions)
        {
            return analyzerActions.SymbolActionsCount > 0 ||
                analyzerActions.SyntaxNodeActionsCount > 0 ||
                analyzerActions.SemanticModelActionsCount > 0 ||
                analyzerActions.CodeBlockActionsCount > 0 ||
                analyzerActions.CodeBlockStartActionsCount > 0;
        }

        public static async Task<bool> SupportsSyntaxDiagnosticAnalysisAsync(this DiagnosticAnalyzer analyzer, DiagnosticAnalyzerDriver driver)
        {
            var category = await analyzer.GetDiagnosticAnalyzerCategoryAsync(driver).ConfigureAwait(false);
            return (category & DiagnosticAnalyzerCategory.SyntaxAnalysis) != 0;
        }

        public static async Task<bool> SupportsSemanticDiagnosticAnalysisAsync(this DiagnosticAnalyzer analyzer, DiagnosticAnalyzerDriver driver)
        {
            var category = await analyzer.GetDiagnosticAnalyzerCategoryAsync(driver).ConfigureAwait(false);
            return (category & (DiagnosticAnalyzerCategory.SemanticSpanAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis)) != 0;
        }

        public static async Task<bool> SupportsSpanBasedSemanticDiagnosticAnalysisAsync(this DiagnosticAnalyzer analyzer, DiagnosticAnalyzerDriver driver)
        {
            var category = await analyzer.GetDiagnosticAnalyzerCategoryAsync(driver).ConfigureAwait(false);
            return (category & DiagnosticAnalyzerCategory.SemanticSpanAnalysis) != 0;
        }

        public static async Task<bool> SupportsProjectDiagnosticAnalysisAsync(this DiagnosticAnalyzer analyzer, DiagnosticAnalyzerDriver driver)
        {
            var category = await analyzer.GetDiagnosticAnalyzerCategoryAsync(driver).ConfigureAwait(false);
            return (category & DiagnosticAnalyzerCategory.ProjectAnalysis) != 0;
        }
    }
}
