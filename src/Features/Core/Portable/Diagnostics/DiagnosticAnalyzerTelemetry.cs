﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class DiagnosticAnalyzerTelemetry
    {
        private readonly struct Data
        {
            public readonly int CompilationStartActionsCount;
            public readonly int CompilationEndActionsCount;
            public readonly int CompilationActionsCount;
            public readonly int SyntaxTreeActionsCount;
            public readonly int SemanticModelActionsCount;
            public readonly int SymbolActionsCount;
            public readonly int SymbolStartActionsCount;
            public readonly int SymbolEndActionsCount;
            public readonly int SyntaxNodeActionsCount;
            public readonly int CodeBlockStartActionsCount;
            public readonly int CodeBlockEndActionsCount;
            public readonly int CodeBlockActionsCount;
            public readonly int OperationActionsCount;
            public readonly int OperationBlockStartActionsCount;
            public readonly int OperationBlockEndActionsCount;
            public readonly int OperationBlockActionsCount;
            public readonly int SuppressionActionsCount;

            public readonly bool IsTelemetryCollectionAllowed;

            public Data(AnalyzerTelemetryInfo analyzerTelemetryInfo, bool isTelemetryCollectionAllowed)
            {
                CodeBlockActionsCount = analyzerTelemetryInfo.CodeBlockActionsCount;
                CodeBlockEndActionsCount = analyzerTelemetryInfo.CodeBlockEndActionsCount;
                CodeBlockStartActionsCount = analyzerTelemetryInfo.CodeBlockStartActionsCount;
                CompilationActionsCount = analyzerTelemetryInfo.CompilationActionsCount;
                CompilationEndActionsCount = analyzerTelemetryInfo.CompilationEndActionsCount;
                CompilationStartActionsCount = analyzerTelemetryInfo.CompilationStartActionsCount;
                SemanticModelActionsCount = analyzerTelemetryInfo.SemanticModelActionsCount;
                SymbolActionsCount = analyzerTelemetryInfo.SymbolActionsCount;
                SyntaxNodeActionsCount = analyzerTelemetryInfo.SyntaxNodeActionsCount;
                SyntaxTreeActionsCount = analyzerTelemetryInfo.SyntaxTreeActionsCount;
                OperationActionsCount = analyzerTelemetryInfo.OperationActionsCount;
                OperationBlockActionsCount = analyzerTelemetryInfo.OperationBlockActionsCount;
                OperationBlockEndActionsCount = analyzerTelemetryInfo.OperationBlockEndActionsCount;
                OperationBlockStartActionsCount = analyzerTelemetryInfo.OperationBlockStartActionsCount;
                SymbolStartActionsCount = analyzerTelemetryInfo.SymbolStartActionsCount;
                SymbolEndActionsCount = analyzerTelemetryInfo.SymbolEndActionsCount;
                SuppressionActionsCount = analyzerTelemetryInfo.SuppressionActionsCount;

                IsTelemetryCollectionAllowed = isTelemetryCollectionAllowed;
            }
        }

        private readonly object _guard = new object();
        private ImmutableDictionary<Type, Data> _analyzerInfoMap;

        public DiagnosticAnalyzerTelemetry()
        {
            _analyzerInfoMap = ImmutableDictionary<Type, Data>.Empty;
        }

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
                        m["Analyzer.NameHashCode"] = ComputeSha256Hash(analyzerName);
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

        private static string ComputeSha256Hash(string name)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(name)));
        }
    }
}
