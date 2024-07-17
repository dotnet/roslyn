// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal static class SupportedChangesServiceExtensions
{
    public static bool CanApplyChange(this Solution solution, ApplyChangesKind kind)
        => solution.Services.GetRequiredService<ISupportedChangesService>().CanApplyChange(kind);

    public static bool CanApplyParseOptionChange(this Project project, ParseOptions oldOptions, ParseOptions newOptions)
        => project.Solution.Services.GetRequiredService<ISupportedChangesService>().CanApplyParseOptionChange(oldOptions, newOptions, project);
}
