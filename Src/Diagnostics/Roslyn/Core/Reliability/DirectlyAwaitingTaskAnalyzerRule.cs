// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Roslyn.Diagnostics.Analyzers
{
    internal static class DirectlyAwaitingTaskAnalyzerRule
    {
        private static LocalizableString localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.DirectlyAwaitingTaskDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.DirectlyAwaitingTaskMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        
        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.DirectlyAwaitingTaskAnalyzerRuleId,
            localizableTitle,
            localizableMessage,
            "Reliability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);
    }
}
