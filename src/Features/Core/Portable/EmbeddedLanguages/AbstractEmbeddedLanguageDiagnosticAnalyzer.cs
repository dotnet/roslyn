// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
{
    internal abstract class AbstractEmbeddedLanguageDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private readonly ImmutableArray<AbstractBuiltInCodeStyleDiagnosticAnalyzer> _analyzers;
        private readonly DiagnosticAnalyzerCategory _category;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        protected AbstractEmbeddedLanguageDiagnosticAnalyzer(
            IEmbeddedLanguageFeaturesProvider languagesProvider)
        {
            var supportedDiagnostics = ArrayBuilder<DiagnosticDescriptor>.GetInstance();

            var analyzers = ArrayBuilder<AbstractBuiltInCodeStyleDiagnosticAnalyzer>.GetInstance();

            var category = default(DiagnosticAnalyzerCategory?);
            Debug.Assert(languagesProvider.Languages.Length > 0);
            foreach (var language in languagesProvider.Languages)
            {
                var analyzer = language.DiagnosticAnalyzer;
                if (analyzer != null)
                {
                    analyzers.Add(analyzer);
                    supportedDiagnostics.AddRange(analyzer.SupportedDiagnostics);

                    category = category ?? analyzer.GetAnalyzerCategory();
                    Debug.Assert(category == analyzer.GetAnalyzerCategory(),
                        "All embedded analyzers must have the same analyzer category.");
                }
            }

            _category = category.Value;
            _analyzers = analyzers.ToImmutableAndFree();
            this.SupportedDiagnostics = supportedDiagnostics.ToImmutableAndFree();
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => _category;

        public bool OpenFileOnly(Workspace workspace)
            => _analyzers.Any(a => a.OpenFileOnly(workspace));

        public override void Initialize(AnalysisContext context)
        {
            foreach (var analyzer in _analyzers)
            {
                analyzer.Initialize(context);
            }
        }
    }
}
