// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class DiagnosticCustomTags
{
    private static readonly string s_enforceOnBuildNeverTag = EnforceOnBuild.Never.ToCustomTag();

    public static string[] Microsoft
    {
        get
        {
            Assert(field, WellKnownDiagnosticTags.Telemetry);
            return field;
        }
    } = [WellKnownDiagnosticTags.Telemetry];

    public static string[] EditAndContinue
    {
        get
        {
            Assert(field, WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag);
            return field;
        }
    } = [WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag];

    public static string[] Unnecessary
    {
        get
        {
            Assert(field, WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry);
            return field;
        }
    } = [WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry];

    public static string[] NotConfigurable
    {
        get
        {
            Assert(field, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry);
            return field;
        }
    } = [WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry];

    public static string[] UnnecessaryAndNotConfigurable
    {
        get
        {
            Assert(field, WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry);
            return field;
        }
    } = [WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry];

    [Conditional("DEBUG")]
    private static void Assert(string[] customTags, params ReadOnlySpan<string> tags)
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
