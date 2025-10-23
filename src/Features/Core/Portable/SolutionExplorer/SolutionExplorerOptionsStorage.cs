// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionExplorer;

internal static class SolutionExplorerOptionsStorage
{
    public static SolutionExplorerOptions GetSolutionExplorerOptions(this IGlobalOptionService globalOptions, string language)
        => new()
        {
            ShowSymbols = globalOptions.GetOption(ShowSymbols, language)
        };

    private static readonly OptionGroup s_solutionExplorerGroup = new(name: "solution_explorer", description: "");

    public static readonly PerLanguageOption2<bool> ShowSymbols = new(
        "dotnet_solution_explorer_show_symbols", SolutionExplorerOptions.Default.ShowSymbols, group: s_solutionExplorerGroup);
}
