// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal static class WellKnownDiagnosticTagsExtensions
    {
        public const string Dataflow = nameof(Dataflow);
        public const string CompilationEnd = nameof(CompilationEnd);
        public static string[] DataflowAndTelemetry = new string[] { Dataflow, WellKnownDiagnosticTags.Telemetry };
        public static string[] Telemetry = new string[] { WellKnownDiagnosticTags.Telemetry };
        public static string[] CompilationEndAndTelemetry = new string[] { CompilationEnd, WellKnownDiagnosticTags.Telemetry };
    }
}
