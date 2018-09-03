// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
{
    internal abstract class AbstractEmbeddedLanguageDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private readonly ImmutableArray<AbstractCodeStyleDiagnosticAnalyzer> _analyzers;

        protected AbstractEmbeddedLanguageDiagnosticAnalyzer(
            IEmbeddedLanguageFeaturesProvider languagesProvider)
        {
            var supportedDiagnostics = ArrayBuilder<DiagnosticDescriptor>.GetInstance();

            var analyzers = ArrayBuilder<AbstractCodeStyleDiagnosticAnalyzer>.GetInstance();

            var analyzerCategory = default(DiagnosticAnalyzerCategory?);
            foreach (var language in languagesProvider.Languages)
            {
                foreach (var analyzer in language.DiagnosticAnalyzers)
                {
                    analyzers.Add(analyzer);
                    supportedDiagnostics.AddRange(analyzer.SupportedDiagnostics);

                    analyzerCategory = analyzerCategory ?? analyzer.GetAnalyzerCategory();

                    if (analyzerCategory != analyzer.GetAnalyzerCategory())
                    {
                        throw new InvalidOperationException("All diagnostic analyzers must share the same category.");
                    }
                }
            }

            _analyzers = analyzers.ToImmutableAndFree();
            this.SupportedDiagnostics = supportedDiagnostics.ToImmutableAndFree();
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => _analyzers[0].GetAnalyzerCategory();

        public bool OpenFileOnly(Workspace workspace)
            => _analyzers[0].OpenFileOnly(workspace);

        public override void Initialize(AnalysisContext context)
        {
            foreach (var analyzer in _analyzers)
            {
                analyzer.Initialize(context);
            }
        }
    }
}
