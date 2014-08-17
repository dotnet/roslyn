// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Roslyn.Diagnostics.Analyzers
{
    internal static class DirectlyAwaitingTaskAnalyzerRule
    {
        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.DirectlyAwaitingTaskAnalyzerRuleId,
            RoslynDiagnosticsResources.DirectlyAwaitingTaskDescription,
            RoslynDiagnosticsResources.DirectlyAwaitingTaskMessage,
            "Reliability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);
    }
}
