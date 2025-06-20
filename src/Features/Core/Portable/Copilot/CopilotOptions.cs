// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Copilot;

internal static class CopilotOptions
{
    public static Option2<bool> AnalyzeCopilotChanges { get; } = new Option2<bool>("dotnet_analyze_copilot_changes", defaultValue: false);
}
