// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Internal.Log;

#if NETSTANDARD2_0
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed class DiagnosticAnalyzerTelemetry
{
    private readonly struct Data(AnalyzerTelemetryInfo analyzerTelemetryInfo, bool isTelemetryCollectionAllowed)
    {
        public readonly int CompilationStartActionsCount = analyzerTelemetryInfo.CompilationStartActionsCount;
        public readonly int CompilationEndActionsCount = analyzerTelemetryInfo.CompilationEndActionsCount;
        public readonly int CompilationActionsCount = analyzerTelemetryInfo.CompilationActionsCount;
        public readonly int SyntaxTreeActionsCount = analyzerTelemetryInfo.SyntaxTreeActionsCount;
        public readonly int AdditionalFileActionsCount = analyzerTelemetryInfo.AdditionalFileActionsCount;
        public readonly int SemanticModelActionsCount = analyzerTelemetryInfo.SemanticModelActionsCount;
        public readonly int SymbolActionsCount = analyzerTelemetryInfo.SymbolActionsCount;
        public readonly int SymbolStartActionsCount = analyzerTelemetryInfo.SymbolStartActionsCount;
        public readonly int SymbolEndActionsCount = analyzerTelemetryInfo.SymbolEndActionsCount;
        public readonly int SyntaxNodeActionsCount = analyzerTelemetryInfo.SyntaxNodeActionsCount;
        public readonly int CodeBlockStartActionsCount = analyzerTelemetryInfo.CodeBlockStartActionsCount;
        public readonly int CodeBlockEndActionsCount = analyzerTelemetryInfo.CodeBlockEndActionsCount;
        public readonly int CodeBlockActionsCount = analyzerTelemetryInfo.CodeBlockActionsCount;
        public readonly int OperationActionsCount = analyzerTelemetryInfo.OperationActionsCount;
        public readonly int OperationBlockStartActionsCount = analyzerTelemetryInfo.OperationBlockStartActionsCount;
        public readonly int OperationBlockEndActionsCount = analyzerTelemetryInfo.OperationBlockEndActionsCount;
        public readonly int OperationBlockActionsCount = analyzerTelemetryInfo.OperationBlockActionsCount;
        public readonly int SuppressionActionsCount = analyzerTelemetryInfo.SuppressionActionsCount;

        public readonly bool IsTelemetryCollectionAllowed = isTelemetryCollectionAllowed;
    }

    private readonly object _guard = new();
    private ImmutableDictionary<Type, Data> _analyzerInfoMap;

    public DiagnosticAnalyzerTelemetry()
        => _analyzerInfoMap = ImmutableDictionary<Type, Data>.Empty;

    public void UpdateAnalyzerActionsTelemetry(DiagnosticAnalyzer analyzer, AnalyzerTelemetryInfo analyzerTelemetryInfo, bool isTelemetryCollectionAllowed)
    {
        lock (_guard)
        {
            _analyzerInfoMap = _analyzerInfoMap.SetItem(analyzer.GetType(), new Data(analyzerTelemetryInfo, isTelemetryCollectionAllowed));
        }
    }

    public void ReportAndClear(int correlationId)
    {
        ImmutableDictionary<Type, Data> map;
        lock (_guard)
        {
            map = _analyzerInfoMap;
            _analyzerInfoMap = ImmutableDictionary<Type, Data>.Empty;
        }

        foreach (var (analyzerType, analyzerInfo) in map)
        {
            Logger.Log(FunctionId.DiagnosticAnalyzerDriver_AnalyzerTypeCount, KeyValueLogMessage.Create(m =>
            {
                m["Id"] = correlationId;

                var analyzerName = analyzerType.FullName;

                if (analyzerInfo.IsTelemetryCollectionAllowed)
                {
                    // log analyzer name and exception as is:
                    m["Analyzer.Name"] = analyzerName;
                }
                else
                {
                    // annonymize analyzer and exception names:
                    m["Analyzer.NameHashCode"] = AnalyzerNameForTelemetry.ComputeSha256Hash(analyzerName);
                }

                m["Analyzer.CodeBlock"] = analyzerInfo.CodeBlockActionsCount;
                m["Analyzer.CodeBlockStart"] = analyzerInfo.CodeBlockStartActionsCount;
                m["Analyzer.CodeBlockEnd"] = analyzerInfo.CodeBlockEndActionsCount;
                m["Analyzer.Compilation"] = analyzerInfo.CompilationActionsCount;
                m["Analyzer.CompilationStart"] = analyzerInfo.CompilationStartActionsCount;
                m["Analyzer.CompilationEnd"] = analyzerInfo.CompilationEndActionsCount;
                m["Analyzer.SemanticModel"] = analyzerInfo.SemanticModelActionsCount;
                m["Analyzer.SyntaxNode"] = analyzerInfo.SyntaxNodeActionsCount;
                m["Analyzer.SyntaxTree"] = analyzerInfo.SyntaxTreeActionsCount;
                m["Analyzer.AdditionalFile"] = analyzerInfo.AdditionalFileActionsCount;
                m["Analyzer.Operation"] = analyzerInfo.OperationActionsCount;
                m["Analyzer.OperationBlock"] = analyzerInfo.OperationBlockActionsCount;
                m["Analyzer.OperationBlockStart"] = analyzerInfo.OperationBlockStartActionsCount;
                m["Analyzer.OperationBlockEnd"] = analyzerInfo.OperationBlockEndActionsCount;
                m["Analyzer.Symbol"] = analyzerInfo.SymbolActionsCount;
                m["Analyzer.SymbolStart"] = analyzerInfo.SymbolStartActionsCount;
                m["Analyzer.SymbolEnd"] = analyzerInfo.SymbolEndActionsCount;
                m["Analyzer.Suppression"] = analyzerInfo.SuppressionActionsCount;
            }));
        }
    }
}
