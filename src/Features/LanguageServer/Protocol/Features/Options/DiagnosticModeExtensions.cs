// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticModeExtensions
    {
        public static DiagnosticMode GetDiagnosticMode(this IGlobalOptionService globalOptions, Option2<DiagnosticMode> option)
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

        /// <summary>
        /// Gets all the diagnostics for this event, respecting the callers setting on if they're getting it for pull
        /// diagnostics or push diagnostics.  Most clients should use this to ensure they see the proper set of
        /// diagnostics in their scenario (or an empty array if not in their scenario).
        /// </summary>
        public static ImmutableArray<DiagnosticData> GetPullDiagnostics(
            this DiagnosticsUpdatedArgs args, IGlobalOptionService globalOptions, Option2<DiagnosticMode> diagnosticMode)
        {
            // If push diagnostics are on, they get nothing since they're asking for pull diagnostics.
            if (globalOptions.IsPushDiagnostics(diagnosticMode))
                return ImmutableArray<DiagnosticData>.Empty;

            return args.GetAllDiagnosticsRegardlessOfPushPullSetting();
        }

        /// <summary>
        /// Gets all the diagnostics for this event, respecting the callers setting on if they're getting it for pull
        /// diagnostics or push diagnostics.  Most clients should use this to ensure they see the proper set of
        /// diagnostics in their scenario (or an empty array if not in their scenario).
        /// </summary>
        public static ImmutableArray<DiagnosticData> GetPushDiagnostics(
            this DiagnosticsUpdatedArgs args, IGlobalOptionService globalOptions, Option2<DiagnosticMode> diagnosticMode)
        {
            // If pull diagnostics are on, they get nothing since they're asking for push diagnostics.
            if (globalOptions.IsPullDiagnostics(diagnosticMode))
                return ImmutableArray<DiagnosticData>.Empty;

            return args.GetAllDiagnosticsRegardlessOfPushPullSetting();
        }
    }
}
