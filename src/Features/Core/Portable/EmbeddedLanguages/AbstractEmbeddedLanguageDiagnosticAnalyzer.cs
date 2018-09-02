// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
{
    internal abstract class AbstractEmbeddedLanguageDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;

        protected AbstractEmbeddedLanguageDiagnosticAnalyzer(
            IEmbeddedLanguageFeaturesProvider languagesProvider)
        {
            var supportedDiagnostics = ArrayBuilder<DiagnosticDescriptor>.GetInstance();

            var analyzers = ArrayBuilder<DiagnosticAnalyzer>.GetInstance();

            foreach (var language in languagesProvider.GetEmbeddedLanguages())
            {
                var analyzer = language.DiagnosticAnalyzer;
                if (analyzer != null)
                {
                    analyzers.Add(analyzer);
                    supportedDiagnostics.AddRange(analyzer.SupportedDiagnostics);
                }
            }

            _analyzers = analyzers.ToImmutableAndFree();
            this.SupportedDiagnostics = supportedDiagnostics.ToImmutableAndFree();
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => ((IBuiltInAnalyzer)_analyzers[0]).GetAnalyzerCategory();

        public bool OpenFileOnly(Workspace workspace)
            => ((IBuiltInAnalyzer)_analyzers[0]).OpenFileOnly(workspace);

        public override void Initialize(AnalysisContext context)
        {
            foreach (var analyzer in _analyzers)
            {
                analyzer.Initialize(context);
            }
        }
    }
}
