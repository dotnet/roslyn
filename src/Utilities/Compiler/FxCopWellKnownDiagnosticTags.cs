// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal static class FxCopWellKnownDiagnosticTags
    {
        public const string PortedFromFxCop = nameof(PortedFromFxCop);

        public static readonly string[] PortedFxCopRule = new string[] { PortedFromFxCop, WellKnownDiagnosticTags.Telemetry };
        public static readonly string[] PortedFxCopRuleEnabledInAggressiveMode = new string[] { PortedFromFxCop, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTagsExtensions.EnabledRuleInAggressiveMode };

        public static readonly string[] PortedFxCopDataflowRule = new string[] { PortedFromFxCop, WellKnownDiagnosticTagsExtensions.Dataflow, WellKnownDiagnosticTags.Telemetry };
        public static readonly string[] PortedFxCopDataflowRuleEnabledInAggressiveMode = new string[] { PortedFromFxCop, WellKnownDiagnosticTagsExtensions.Dataflow, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTagsExtensions.EnabledRuleInAggressiveMode };
    }
}