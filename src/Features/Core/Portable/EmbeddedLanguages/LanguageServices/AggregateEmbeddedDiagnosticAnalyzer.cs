//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System.Collections.Immutable;
//using Microsoft.CodeAnalysis.Diagnostics;
//using Microsoft.CodeAnalysis.Options;
//using Microsoft.CodeAnalysis.PooledObjects;

//namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
//{
//    /// <summary>
//    /// An <see cref="IEmbeddedDiagnosticAnalyzer"/> built out of many individual 
//    /// <see cref="IEmbeddedDiagnosticAnalyzer"/>s
//    /// </summary>
//    internal class AggregateEmbeddedDiagnosticAnalyzer : IEmbeddedDiagnosticAnalyzer
//    {
//        private readonly ImmutableArray<IEmbeddedDiagnosticAnalyzer> _analyzers;

//        public AggregateEmbeddedDiagnosticAnalyzer(
//            params IEmbeddedDiagnosticAnalyzer[] analyzers)
//        {
//            _analyzers = analyzers.ToImmutableArray();

//            var diagnostics = ArrayBuilder<DiagnosticDescriptor>.GetInstance();
//            foreach (var analyzer in _analyzers)
//            {
//                diagnostics.AddRange(analyzer.SupportedDiagnostics);
//            }

//            this.SupportedDiagnostics = diagnostics.ToImmutableAndFree();
//        }

//        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

//        public void Analyze(SemanticModelAnalysisContext context, OptionSet options)
//        {
//            foreach (var analyer in _analyzers)
//            {
//                analyer.Analyze(context, options);
//            }
//        }
//    }
//}
