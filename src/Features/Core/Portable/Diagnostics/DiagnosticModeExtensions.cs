// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticModeExtensions
    {
        private static DiagnosticMode GetDiagnosticMode(IGlobalOptionService globalOptions, Option2<DiagnosticMode> option)
        {
            var diagnosticModeOption = globalOptions.GetOption(option);

            // If the workspace diagnostic mode is set to Default, defer to the feature flag service.
            if (diagnosticModeOption == DiagnosticMode.Default)
            {
                return globalOptions.GetOption(DiagnosticOptions.LspPullDiagnosticsFeatureFlag) ? DiagnosticMode.Pull : DiagnosticMode.Push;
            }

            // Otherwise, defer to the workspace+option to determine what mode we're in.
            return diagnosticModeOption;
        }

        public static bool IsPullDiagnostics(this IGlobalOptionService globalOptions, Option2<DiagnosticMode> option)
            => GetDiagnosticMode(globalOptions, option) == DiagnosticMode.Pull;

        public static bool IsPushDiagnostics(this IGlobalOptionService globalOptions, Option2<DiagnosticMode> option)
            => GetDiagnosticMode(globalOptions, option) == DiagnosticMode.Push;
    }
}
