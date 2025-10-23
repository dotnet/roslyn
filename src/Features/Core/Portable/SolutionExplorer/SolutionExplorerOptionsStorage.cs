// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionExplorer;

internal static class SolutionExplorerOptionsStorage
{
    public static SolutionExplorerOptions GetSolutionExplorerOptions(this IGlobalOptionService globalOptions)
        => new()
        {
            ShowLanguageSymbolsInsideSolutionExplorerFiles = globalOptions.GetOption(ShowLanguageSymbolsInsideSolutionExplorerFiles)
        };

    private static readonly OptionGroup s_solutionExplorerGroup = new(name: "solution_explorer", description: "");

    public static readonly Option2<bool> ShowLanguageSymbolsInsideSolutionExplorerFiles = new(
        "dotnet_solution_explorer_show_language_symbols_inside_solution_explorer_files", SolutionExplorerOptions.Default.ShowLanguageSymbolsInsideSolutionExplorerFiles, group: s_solutionExplorerGroup);
}
