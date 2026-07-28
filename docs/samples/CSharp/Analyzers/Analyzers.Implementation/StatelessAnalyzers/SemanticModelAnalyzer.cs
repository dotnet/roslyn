// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sample.Analyzers
{
    /// <summary>
    /// Analyzer for reporting syntax tree diagnostics, that require some semantic analysis.
    /// It reports diagnostics for all source files which have at least one declaration diagnostic.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SemanticModelAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Source file declaration diagnostics count";
        public const string MessageFormat = "Source file '{0}' has '{1}' declaration diagnostic(s)";
        private const string Description = "Source file declaration diagnostic count.";

        internal static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                DiagnosticIds.SemanticModelAnalyzerRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.Stateless,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        }

        private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            // Find just those source files with declaration diagnostics.
            int diagnosticsCount = context.SemanticModel.GetDeclarationDiagnostics().Length;
            if (diagnosticsCount > 0)
            {
                // For all such files, produce a diagnostic.
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.None,
                        Path.GetFileName(context.SemanticModel.SyntaxTree.FilePath),
                        diagnosticsCount));
            }
        }
    }
}
