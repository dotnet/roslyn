// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticCustomTags
    {
        private static readonly string s_enforceOnBuildNeverTag = EnforceOnBuild.Never.ToCustomTag();

        private static readonly string[] s_microsoftCustomTags = [WellKnownDiagnosticTags.Telemetry];
        private static readonly string[] s_editAndContinueCustomTags = [WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag];
        private static readonly string[] s_unnecessaryCustomTags = [WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry];
        private static readonly string[] s_notConfigurableCustomTags = [WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry];
        private static readonly string[] s_unnecessaryAndNotConfigurableCustomTags = [WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry];

        public static string[] Microsoft
        {
            get
            {
                Assert(s_microsoftCustomTags, WellKnownDiagnosticTags.Telemetry);
                return s_microsoftCustomTags;
            }
        }

        public static string[] EditAndContinue
        {
            get
            {
                Assert(s_editAndContinueCustomTags, WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag);
                return s_editAndContinueCustomTags;
            }
        }

        public static string[] Unnecessary
        {
            get
            {
                Assert(s_unnecessaryCustomTags, WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry);
                return s_unnecessaryCustomTags;
            }
        }

        public static string[] NotConfigurable
        {
            get
            {
                Assert(s_notConfigurableCustomTags, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry);
                return s_notConfigurableCustomTags;
            }
        }

        public static string[] UnnecessaryAndNotConfigurable
        {
            get
            {
                Assert(s_unnecessaryAndNotConfigurableCustomTags, WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry);
                return s_unnecessaryAndNotConfigurableCustomTags;
            }
        }

        [Conditional("DEBUG")]
        private static void Assert(string[] customTags, params string[] tags)
        {
            Debug.Assert(customTags.Length == tags.Length);

            for (var i = 0; i < tags.Length; i++)
            {
                Debug.Assert(customTags[i] == tags[i]);
            }
        }

        internal static string[] Create(bool isUnnecessary, bool isConfigurable, bool isCustomConfigurable, EnforceOnBuild enforceOnBuild)
        {
            Debug.Assert(isConfigurable || enforceOnBuild == EnforceOnBuild.Never);

            var customTagsBuilder = ImmutableArray.CreateBuilder<string>();
            customTagsBuilder.AddRange(Microsoft);

            customTagsBuilder.Add(enforceOnBuild.ToCustomTag());

            if (!isConfigurable)
            {
                customTagsBuilder.Add(WellKnownDiagnosticTags.NotConfigurable);
            }
            else if (isCustomConfigurable)
            {
                customTagsBuilder.Add(WellKnownDiagnosticTags.CustomSeverityConfigurable);
            }

            if (isUnnecessary)
            {
                customTagsBuilder.Add(WellKnownDiagnosticTags.Unnecessary);
            }

            return customTagsBuilder.ToArray();
        }
    }
}
