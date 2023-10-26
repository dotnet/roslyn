// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeAnalysisSuggestions;

internal sealed class CodeAnalysisSuggestionsOptionsStorage
{
    public static readonly Option2<bool> ShowCodeAnalysisSuggestionsInLightbulb = new("dotnet_show_code_analysis_suggestions_in_lightbulb", defaultValue: true);

    public static readonly Option2<bool> HasMetCandidacyRequirementsForCodeQuality = new("dotnet_has_met_code_quality_candidacy", defaultValue: false);
    public static readonly Option2<bool> HasMetCandidacyRequirementsForCodeStyle = new("dotnet_has_met_code_style_candidacy", defaultValue: false);

    public static readonly Option2<long> LastDateTimeUsedCodeQualityFix = new("dotnet_last_date_time_used_code_quality_fix", defaultValue: DateTime.MinValue.ToBinary());
    public static readonly Option2<long> LastDateTimeUsedCodeStyleFix = new("dotnet_last_date_time_used_code_style_fix", defaultValue: DateTime.MinValue.ToBinary());

    public static readonly Option2<int> InvokedCodeQualityFixCount = new("dotnet_invoked_code_quality_fix_count", defaultValue: 0);
    public static readonly Option2<int> InvokedCodeStyleFixCount = new("dotnet_invoked_code_style_fix_count", defaultValue: 0);
}
