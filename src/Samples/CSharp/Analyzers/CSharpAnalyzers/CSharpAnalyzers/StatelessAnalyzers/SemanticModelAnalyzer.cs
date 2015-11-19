// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpAnalyzers
{
    /// <summary>
    /// Analyzer for reporting syntax tree diagnostics, that require some semantic analysis.
    /// It reports diagnostics for all source files which have at least one declaration diagnostic.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SemanticModelAnalyzer : DiagnosticAnalyzer
    {
        #region Descriptor fields

        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.SemanticModelAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.SemanticModelAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.SemanticModelAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.SemanticModelAnalyzerRuleId, Title, MessageFormat, DiagnosticCategories.Stateless, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        }

        private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            // Find just those source files with declaration diagnostics.
            var diagnosticsCount = context.SemanticModel.GetDeclarationDiagnostics().Length;
            if (diagnosticsCount > 0)
            {
                // For all such files, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule,  Location.None, Path.GetFileName(context.SemanticModel.SyntaxTree.FilePath), diagnosticsCount);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
