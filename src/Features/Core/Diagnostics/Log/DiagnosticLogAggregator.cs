// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Diagnostics.Log
{
    internal class DiagnosticLogAggregator : LogAggregator
    {
        public static readonly string[] AnalyzerTypes =
        {
            "Analyzer.CodeBlockEnd",
            "Analyzer.CodeBlockStart",
            "Analyzer.CompilationEnd",
            "Analyzer.CompilationStart",
            "Analyzer.SemanticModel",
            "Analyzer.Symbol",
            "Analyzer.SyntaxNode",
            "Analyzer.SyntaxTree"
        };

        private readonly DiagnosticAnalyzerService _owner;
        private ImmutableDictionary<Type, AnalyzerInfo> _analyzerInfoMap;

        public DiagnosticLogAggregator(DiagnosticAnalyzerService owner)
        {
            _owner = owner;
            _analyzerInfoMap = ImmutableDictionary<Type, AnalyzerInfo>.Empty;
        }

        public IEnumerable<KeyValuePair<Type, AnalyzerInfo>> AnalyzerInfoMap
        {
            get { return _analyzerInfoMap; }
        }

        public void UpdateAnalyzerTypeCount(DiagnosticAnalyzer analyzer, AnalyzerActions analyzerActions)
        {
            var telemetry = DiagnosticAnalyzerLogger.AllowsTelemetry(_owner, analyzer);

            ImmutableInterlocked.AddOrUpdate(
                ref _analyzerInfoMap,
                analyzer.GetType(),
                addValue: new AnalyzerInfo(analyzer, analyzerActions, telemetry),
                updateValueFactory: (k, ai) =>
                {
                    ai.SetAnalyzerTypeCount(analyzerActions);
                    return ai;
                });
        }

        public class AnalyzerInfo
        {
            public Type CLRType;
            public bool Telemetry;
            public int[] Counts = new int[DiagnosticLogAggregator.AnalyzerTypes.Length];

            public AnalyzerInfo(DiagnosticAnalyzer analyzer, AnalyzerActions analyzerActions, bool telemetry)
            {
                CLRType = analyzer.GetType();
                Telemetry = telemetry;

                Counts[0] = analyzerActions.CodeBlockEndActionsCount;
                Counts[1] = analyzerActions.CodeBlockStartActionsCount;
                Counts[2] = analyzerActions.CompilationEndActionsCount;
                Counts[3] = analyzerActions.CompilationStartActionsCount;
                Counts[4] = analyzerActions.SemanticModelActionsCount;
                Counts[5] = analyzerActions.SymbolActionsCount;
                Counts[6] = analyzerActions.SyntaxNodeActionsCount;
                Counts[7] = analyzerActions.SyntaxTreeActionsCount;
            }

            public void SetAnalyzerTypeCount(AnalyzerActions analyzerActions)
            {
                Counts[0] = analyzerActions.CodeBlockEndActionsCount;
                Counts[1] = analyzerActions.CodeBlockStartActionsCount;
                Counts[2] = analyzerActions.CompilationEndActionsCount;
                Counts[3] = analyzerActions.CompilationStartActionsCount;
                Counts[4] = analyzerActions.SemanticModelActionsCount;
                Counts[5] = analyzerActions.SymbolActionsCount;
                Counts[6] = analyzerActions.SyntaxNodeActionsCount;
                Counts[7] = analyzerActions.SyntaxTreeActionsCount;
            }
        }
    }
}
