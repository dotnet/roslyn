// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages
{
    internal abstract class AbstractEmbeddedLanguageDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private readonly ImmutableArray<IEmbeddedDiagnosticAnalyzer> _analyzers;

        protected AbstractEmbeddedLanguageDiagnosticAnalyzer(
            IEmbeddedLanguagesProvider languagesProvider)
        {
            var supportedDiagnostics = ArrayBuilder<DiagnosticDescriptor>.GetInstance();

            var analyzers = ArrayBuilder<IEmbeddedDiagnosticAnalyzer>.GetInstance();

            if (languagesProvider != null)
            {
                foreach (var language in languagesProvider.GetEmbeddedLanguages())
                {
                    var analyzer = language.DiagnosticAnalyzer;
                    if (analyzer != null)
                    {
                        analyzers.Add(analyzer);
                        supportedDiagnostics.AddRange(analyzer.SupportedDiagnostics);
                    }
                }
            }

            _analyzers = analyzers.ToImmutableAndFree();
            this.SupportedDiagnostics = supportedDiagnostics.ToImmutableAndFree();
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public bool OpenFileOnly(Workspace workspace)
            => false;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        }

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var options = context.Options;
            var optionSet = options.GetDocumentOptionSetAsync(
                context.SemanticModel.SyntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            foreach (var analyzer in _analyzers)
            {
                analyzer.Analyze(context, optionSet);
            }
        }
    }
}
