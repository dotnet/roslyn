// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal static class FxCopWellKnownDiagnosticTags
    {
        public const string PortedFromFxCop = nameof(PortedFromFxCop);

        public static readonly string[] PortedFxCopRule = [PortedFromFxCop, WellKnownDiagnosticTags.Telemetry];
        public static readonly string[] PortedFxCopRuleEnabledInAggressiveMode = [PortedFromFxCop, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTagsExtensions.EnabledRuleInAggressiveMode];

        public static readonly string[] PortedFxCopDataflowRule = [PortedFromFxCop, WellKnownDiagnosticTagsExtensions.Dataflow, WellKnownDiagnosticTags.Telemetry];
        public static readonly string[] PortedFxCopDataflowRuleEnabledInAggressiveMode = [PortedFromFxCop, WellKnownDiagnosticTagsExtensions.Dataflow, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTagsExtensions.EnabledRuleInAggressiveMode];
    }
}
