// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using static Microsoft.CodeAnalysis.Diagnostics.Telemetry.AnalyzerTelemetry;

namespace Microsoft.CodeAnalysis.Diagnostics.Log
{
    internal class DiagnosticLogAggregator : LogAggregator
    {
        public static readonly string[] AnalyzerTypes =
        {
            "Analyzer.CodeBlock",
            "Analyzer.CodeBlockEnd",
            "Analyzer.CodeBlockStart",
            "Analyzer.Compilation",
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

        public void UpdateAnalyzerTypeCount(DiagnosticAnalyzer analyzer, ActionCounts analyzerActions, Project projectOpt)
        {
            var telemetry = DiagnosticAnalyzerLogger.AllowsTelemetry(_owner, analyzer, projectOpt?.Id);

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
            public int[] Counts = new int[AnalyzerTypes.Length];

            public AnalyzerInfo(DiagnosticAnalyzer analyzer, ActionCounts analyzerActions, bool telemetry)
            {
                CLRType = analyzer.GetType();
                Telemetry = telemetry;

                Counts[0] = analyzerActions.CodeBlockActionsCount;
                Counts[1] = analyzerActions.CodeBlockEndActionsCount;
                Counts[2] = analyzerActions.CodeBlockStartActionsCount;
                Counts[3] = analyzerActions.CompilationActionsCount;
                Counts[4] = analyzerActions.CompilationEndActionsCount;
                Counts[5] = analyzerActions.CompilationStartActionsCount;
                Counts[6] = analyzerActions.SemanticModelActionsCount;
                Counts[7] = analyzerActions.SymbolActionsCount;
                Counts[8] = analyzerActions.SyntaxNodeActionsCount;
                Counts[9] = analyzerActions.SyntaxTreeActionsCount;
            }

            public void SetAnalyzerTypeCount(ActionCounts analyzerActions)
            {
                Counts[0] = analyzerActions.CodeBlockActionsCount;
                Counts[1] = analyzerActions.CodeBlockEndActionsCount;
                Counts[2] = analyzerActions.CodeBlockStartActionsCount;
                Counts[3] = analyzerActions.CompilationActionsCount;
                Counts[4] = analyzerActions.CompilationEndActionsCount;
                Counts[5] = analyzerActions.CompilationStartActionsCount;
                Counts[6] = analyzerActions.SemanticModelActionsCount;
                Counts[7] = analyzerActions.SymbolActionsCount;
                Counts[8] = analyzerActions.SyntaxNodeActionsCount;
                Counts[9] = analyzerActions.SyntaxTreeActionsCount;
            }
        }
    }
}
