// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal static class FxCopWellKnownDiagnosticTags
    {
        public const string PortedFromFxCop = nameof(PortedFromFxCop);

        public static readonly string[] PortedFxCopRule = new string[] { PortedFromFxCop, WellKnownDiagnosticTags.Telemetry };

        public static readonly string[] PortedFxCopDataflowRule = new string[] { PortedFromFxCop, WellKnownDiagnosticTagsExtensions.Dataflow, WellKnownDiagnosticTags.Telemetry };

        public static bool IsPortedFxCopRule(DiagnosticDescriptor diagnosticDescriptor)
        {
            var result = diagnosticDescriptor.CustomTags.Any(t => t == PortedFromFxCop);
            Debug.Assert(!result || diagnosticDescriptor.Id.StartsWith("CA", StringComparison.OrdinalIgnoreCase));
            return result;
        }
    }
}