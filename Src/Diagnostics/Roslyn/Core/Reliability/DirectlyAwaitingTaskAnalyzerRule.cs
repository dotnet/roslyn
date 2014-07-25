// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Roslyn.Diagnostics.Analyzers
{
    internal static class DirectlyAwaitingTaskAnalyzerRule
    {
        public const string Id = "RS0003";

        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            RoslynDiagnosticsResources.DirectlyAwaitingTaskDescription,
            RoslynDiagnosticsResources.DirectlyAwaitingTaskMessage,
            "Reliability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);
    }
}
