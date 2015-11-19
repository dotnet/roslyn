// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Internal.Log;

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

        public void UpdateAnalyzerTypeCount(DiagnosticAnalyzer analyzer, AnalyzerTelemetryInfo analyzerTelemetryInfo, Project projectOpt)
        {
            var telemetry = DiagnosticAnalyzerLogger.AllowsTelemetry(_owner, analyzer, projectOpt?.Id);

            ImmutableInterlocked.AddOrUpdate(
                ref _analyzerInfoMap,
                analyzer.GetType(),
                addValue: new AnalyzerInfo(analyzer, analyzerTelemetryInfo, telemetry),
                updateValueFactory: (k, ai) =>
                {
                    ai.SetAnalyzerTypeCount(analyzerTelemetryInfo);
                    return ai;
                });
        }

        public class AnalyzerInfo
        {
            public Type CLRType;
            public bool Telemetry;
            public int[] Counts = new int[AnalyzerTypes.Length];

            public AnalyzerInfo(DiagnosticAnalyzer analyzer, AnalyzerTelemetryInfo analyzerTelemetryInfo, bool telemetry)
            {
                CLRType = analyzer.GetType();
                Telemetry = telemetry;

                Counts[0] = analyzerTelemetryInfo.CodeBlockActionsCount;
                Counts[1] = analyzerTelemetryInfo.CodeBlockEndActionsCount;
                Counts[2] = analyzerTelemetryInfo.CodeBlockStartActionsCount;
                Counts[3] = analyzerTelemetryInfo.CompilationActionsCount;
                Counts[4] = analyzerTelemetryInfo.CompilationEndActionsCount;
                Counts[5] = analyzerTelemetryInfo.CompilationStartActionsCount;
                Counts[6] = analyzerTelemetryInfo.SemanticModelActionsCount;
                Counts[7] = analyzerTelemetryInfo.SymbolActionsCount;
                Counts[8] = analyzerTelemetryInfo.SyntaxNodeActionsCount;
                Counts[9] = analyzerTelemetryInfo.SyntaxTreeActionsCount;
            }

            public void SetAnalyzerTypeCount(AnalyzerTelemetryInfo analyzerTelemetryInfo)
            {
                Counts[0] = analyzerTelemetryInfo.CodeBlockActionsCount;
                Counts[1] = analyzerTelemetryInfo.CodeBlockEndActionsCount;
                Counts[2] = analyzerTelemetryInfo.CodeBlockStartActionsCount;
                Counts[3] = analyzerTelemetryInfo.CompilationActionsCount;
                Counts[4] = analyzerTelemetryInfo.CompilationEndActionsCount;
                Counts[5] = analyzerTelemetryInfo.CompilationStartActionsCount;
                Counts[6] = analyzerTelemetryInfo.SemanticModelActionsCount;
                Counts[7] = analyzerTelemetryInfo.SymbolActionsCount;
                Counts[8] = analyzerTelemetryInfo.SyntaxNodeActionsCount;
                Counts[9] = analyzerTelemetryInfo.SyntaxTreeActionsCount;
            }
        }
    }
}
