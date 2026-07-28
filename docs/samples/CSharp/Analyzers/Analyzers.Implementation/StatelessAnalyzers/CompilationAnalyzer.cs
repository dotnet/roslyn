// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sample.Analyzers
{
    /// <summary>
    /// Analyzer for reporting compilation diagnostics.
    /// It reports diagnostics for analyzer diagnostics that have been suppressed for the entire compilation.
    /// </summary>
    /// <remarks>
    /// For analyzers that requires analyzing symbols or syntax nodes across compilation, see <see cref="CompilationStartedAnalyzer"/> and <see cref="CompilationStartedAnalyzerWithCompilationWideAnalysis"/>.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CompilationAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Dont suppress analyzer diagnostics";
        public const string MessageFormat = "Analyzer diagnostic '{0}' is suppressed, consider removing this compilation wide suppression.";
        private const string Description = "Dont suppress analyzer diagnostics.";

        internal static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                DiagnosticIds.CompilationAnalyzerRuleId,
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
            context.RegisterCompilationAction(AnalyzeCompilation);
        }

        private static void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            // Get all the suppressed analyzer diagnostic IDs.
            IEnumerable<string> suppressedAnalyzerDiagnosticIds = GetSuppressedAnalyzerDiagnosticIds(context.Compilation.Options.SpecificDiagnosticOptions);

            foreach (string suppressedDiagnosticId in suppressedAnalyzerDiagnosticIds)
            {
                // For all such suppressed diagnostic IDs, produce a diagnostic.
                Diagnostic diagnostic = Diagnostic.Create(Rule, Location.None, suppressedDiagnosticId);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static IEnumerable<string> GetSuppressedAnalyzerDiagnosticIds(ImmutableDictionary<string, ReportDiagnostic> specificOptions)
        {
            foreach (KeyValuePair<string, ReportDiagnostic> kvp in specificOptions)
            {
                if (kvp.Value == ReportDiagnostic.Suppress)
                {
                    if (kvp.Key.StartsWith("CS", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(kvp.Key.Substring(2), out int intId))
                    {
                        continue;
                    }

                    if (kvp.Key.StartsWith("BC", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(kvp.Key.Substring(2), out intId))
                    {
                        continue;
                    }

                    yield return kvp.Key;
                }
            }
        }
    }
}
