// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpAnalyzers
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
        #region Descriptor fields

        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.CompilationAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.CompilationAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.CompilationAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.CompilationAnalyzerRuleId, Title, MessageFormat, DiagnosticCategories.Stateless, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationAction(AnalyzeCompilation);
        }

        private static void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            // Get all the suppressed analyzer diagnostic IDs.
            var suppressedAnalyzerDiagnosticIds = GetSuppressedAnalyzerDiagnosticIds(context.Compilation.Options.SpecificDiagnosticOptions);

            foreach (var suppressedDiagnosticId in suppressedAnalyzerDiagnosticIds)
            {
                // For all such suppressed diagnostic IDs, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, Location.None, suppressedDiagnosticId);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static IEnumerable<string> GetSuppressedAnalyzerDiagnosticIds(ImmutableDictionary<string, ReportDiagnostic> specificOptions)
        {
            foreach (var kvp in specificOptions)
            {
                if (kvp.Value == ReportDiagnostic.Suppress)
                {
                    int intId;
                    if (kvp.Key.StartsWith("CS", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(kvp.Key.Substring(2), out intId))
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
