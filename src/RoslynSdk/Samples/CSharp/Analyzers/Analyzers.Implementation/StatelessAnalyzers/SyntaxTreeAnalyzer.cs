// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sample.Analyzers
{
    /// <summary>
    /// Analyzer for reporting syntax tree diagnostics.
    /// It reports diagnostics for all source files which have documentation comment diagnostics turned off.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SyntaxTreeAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Do not suppress documentation comment diagnostics";
        public const string MessageFormat = "Enable documentation comment diagnostics on source file '{0}'.";
        private const string Description = "Do not suppress documentation comment diagnostics.";

        internal static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                DiagnosticIds.SyntaxTreeAnalyzerRuleId,
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
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            // Find source files with documentation comment diagnostics turned off.
            if (context.Tree.Options.DocumentationMode != DocumentationMode.Diagnose)
            {
                // For all such files, produce a diagnostic.
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.None,
                        Path.GetFileName(context.Tree.FilePath)));
            }
        }
    }
}
