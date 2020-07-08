// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class DiagnosticDescriptorExtensions
    {
        /// <summary>
        /// Gets project-level effective severity of the given <paramref name="descriptor"/> accounting for severity configurations from both the following sources:
        /// 1. Compilation options from ruleset file, if any, and command line options such as /nowarn, /warnaserror, etc.
        /// 2. Analyzer config documents at the project root directory or in ancestor directories.
        /// </summary>
        public static ReportDiagnostic GetEffectiveSeverity(this DiagnosticDescriptor descriptor,
                                                            CompilationOptions compilationOptions,
                                                            ImmutableDictionary<string, ReportDiagnostic> analyzerConfigSpecificDiagnosticOptions,
                                                            ImmutableDictionary<string, ReportDiagnostic> analyzerConfigGlobalDiagnosticOptions)
        {
            var effectiveSeverity = descriptor.GetEffectiveSeverity(compilationOptions);
            var providedOnCommandLine = compilationOptions.SpecificDiagnosticOptions.ContainsKey(descriptor.Id);

            // Apply the global analyzer config severity, only if it wasn't specified via the command line
            if (!providedOnCommandLine && analyzerConfigGlobalDiagnosticOptions.TryGetValue(descriptor.Id, out var globalReportDiagnostic))
            {
                effectiveSeverity = globalReportDiagnostic;
            }

            // Apply analyzer config options on top of compilation options, unless the diagnostic is explicitly suppressed by compilation options (/nowarn).
            var isSuppressedByCompilationOptions = effectiveSeverity == ReportDiagnostic.Suppress && providedOnCommandLine;
            if (!isSuppressedByCompilationOptions &&
                analyzerConfigSpecificDiagnosticOptions.TryGetValue(descriptor.Id, out var reportDiagnostic))
            {
                effectiveSeverity = reportDiagnostic;
            }

            return effectiveSeverity;
        }
    }
}
