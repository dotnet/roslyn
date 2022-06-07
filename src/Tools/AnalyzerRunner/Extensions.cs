// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            analyzerTelemetryInfo.AdditionalFileActionsCount += addendum.AdditionalFileActionsCount;
            analyzerTelemetryInfo.SuppressionActionsCount += addendum.SuppressionActionsCount;
        }
    }
}
