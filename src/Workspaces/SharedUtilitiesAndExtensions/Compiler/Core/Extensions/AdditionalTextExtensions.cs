// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class AdditionalTextExtensions
{
    public static bool IsGeneratedCode(this AdditionalText additionalText, AnalyzerOptions? analyzerOptions)
    {
        // First check if user has configured "generated_code = true | false" in .editorconfig
        if (analyzerOptions != null)
        {
            var analyzerConfigOptions = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(additionalText);
            var isUserConfiguredGeneratedCode = GeneratedCodeUtilities.GetGeneratedCodeKindFromOptions(analyzerConfigOptions).ToNullable();
            if (isUserConfiguredGeneratedCode.HasValue)
            {
                return isUserConfiguredGeneratedCode.Value;
            }
        }

        // Otherwise, fallback to generated code heuristic.
        return GeneratedCodeUtilities.IsGeneratedCode(additionalText);
    }
}
