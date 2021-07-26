// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal static class WellKnownDiagnosticTagsExtensions
    {
        public const string EnabledRuleInAggressiveMode = nameof(EnabledRuleInAggressiveMode);
        public const string Dataflow = nameof(Dataflow);
        public const string CompilationEnd = nameof(CompilationEnd);
        public static readonly string[] DataflowAndTelemetry = new string[] { Dataflow, WellKnownDiagnosticTags.Telemetry };
        public static readonly string[] DataflowAndTelemetryEnabledInAggressiveMode = new string[] { Dataflow, WellKnownDiagnosticTags.Telemetry, EnabledRuleInAggressiveMode };

        public static readonly string[] Telemetry = new string[] { WellKnownDiagnosticTags.Telemetry };
        public static readonly string[] TelemetryEnabledInAggressiveMode = new string[] { WellKnownDiagnosticTags.Telemetry, EnabledRuleInAggressiveMode };
        public static readonly string[] CompilationEndAndTelemetry = new string[] { CompilationEnd, WellKnownDiagnosticTags.Telemetry };
    }
}
