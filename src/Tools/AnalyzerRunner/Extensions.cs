// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;

namespace AnalyzerRunner
{
    internal static class Extensions
    {
        internal static void Add(this AnalyzerTelemetryInfo analyzerTelemetryInfo, AnalyzerTelemetryInfo addendum)
        {
            analyzerTelemetryInfo.CodeBlockActionsCount += addendum.CodeBlockActionsCount;
            analyzerTelemetryInfo.CodeBlockEndActionsCount += addendum.CodeBlockEndActionsCount;
            analyzerTelemetryInfo.CodeBlockStartActionsCount += addendum.CodeBlockStartActionsCount;
            analyzerTelemetryInfo.CompilationActionsCount += addendum.CompilationActionsCount;
            analyzerTelemetryInfo.CompilationEndActionsCount += addendum.CompilationEndActionsCount;
            analyzerTelemetryInfo.CompilationStartActionsCount += addendum.CompilationStartActionsCount;
            analyzerTelemetryInfo.ExecutionTime += addendum.ExecutionTime;
            analyzerTelemetryInfo.OperationActionsCount += addendum.OperationActionsCount;
            analyzerTelemetryInfo.OperationBlockActionsCount += addendum.OperationBlockActionsCount;
            analyzerTelemetryInfo.OperationBlockEndActionsCount += addendum.OperationBlockEndActionsCount;
            analyzerTelemetryInfo.OperationBlockStartActionsCount += addendum.OperationBlockStartActionsCount;
            analyzerTelemetryInfo.SemanticModelActionsCount += addendum.SemanticModelActionsCount;
            analyzerTelemetryInfo.SymbolActionsCount += addendum.SymbolActionsCount;
            analyzerTelemetryInfo.SymbolStartActionsCount += addendum.SymbolStartActionsCount;
            analyzerTelemetryInfo.SymbolEndActionsCount += addendum.SymbolEndActionsCount;
            analyzerTelemetryInfo.SyntaxNodeActionsCount += addendum.SyntaxNodeActionsCount;
            analyzerTelemetryInfo.SyntaxTreeActionsCount += addendum.SyntaxTreeActionsCount;
            analyzerTelemetryInfo.SuppressionActionsCount += addendum.SuppressionActionsCount;
        }
    }
}
