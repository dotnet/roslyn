// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ValidateFormatString;

internal static class FormatStringValidationOptionStorage
{
    public static readonly PerLanguageOption2<bool> ReportInvalidPlaceholdersInStringDotFormatCalls = new(
        "dotnet_unsupported_report_invalid_placeholders_in_string_dot_format_calls",
        defaultValue: true,
        isEditorConfigOption: true);

    public static readonly ImmutableArray<IOption2> UnsupportedOptions = [ReportInvalidPlaceholdersInStringDotFormatCalls];
}
