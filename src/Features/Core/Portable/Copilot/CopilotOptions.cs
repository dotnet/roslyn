// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Copilot;

internal static class CopilotOptions
{
    public static Option2<bool> AnalyzeCopilotChanges { get; } = new Option2<bool>("dotnet_analyze_copilot_changes", defaultValue: true);
    public static Option2<bool> FixAddMissingImports { get; } = new Option2<bool>("dotnet_copilot_fix_add_missing_imports", defaultValue: false);
    public static Option2<bool> FixAddMissingTokens { get; } = new Option2<bool>("dotnet_copilot_fix_add_missing_tokens", defaultValue: false);
    public static Option2<bool> FixCodeFormat { get; } = new Option2<bool>("dotnet_copilot_fix_code_format", defaultValue: false);
}
