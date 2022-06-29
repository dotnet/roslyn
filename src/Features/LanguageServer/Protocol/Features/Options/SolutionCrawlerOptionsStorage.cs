// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler;

internal static class SolutionCrawlerOptionsStorage
{
    /// <summary>
    /// Option to turn configure background analysis scope for the current user.
    /// </summary>
    public static readonly PerLanguageOption2<BackgroundAnalysisScope> BackgroundAnalysisScopeOption = new(
        "SolutionCrawlerOptionsStorage", "BackgroundAnalysisScopeOption", defaultValue: BackgroundAnalysisScope.Default,
        storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.BackgroundAnalysisScopeOption"));

    /// <summary>
    /// Option to turn configure background analysis scope for the current solution.
    /// </summary>
    public static readonly Option2<BackgroundAnalysisScope?> SolutionBackgroundAnalysisScopeOption = new(
        "SolutionCrawlerOptionsStorage", "SolutionBackgroundAnalysisScopeOption", defaultValue: null);

    /// <summary>
    /// Option to configure compiler diagnostics scope for the current user.
    /// </summary>
    public static readonly PerLanguageOption2<CompilerDiagnosticsScope> CompilerDiagnosticsScopeOption = new(
        "SolutionCrawlerOptionsStorage", "CompilerDiagnosticsScopeOption", defaultValue: CompilerDiagnosticsScope.OpenFiles,
        storageLocation: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.CompilerDiagnosticsScopeOption"));

    public static readonly PerLanguageOption2<bool> RemoveDocumentDiagnosticsOnDocumentClose = new(
        "ServiceFeatureOnOffOptions", "RemoveDocumentDiagnosticsOnDocumentClose", defaultValue: false,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.RemoveDocumentDiagnosticsOnDocumentClose"));

    /// <summary>
    /// Enables forced <see cref="BackgroundAnalysisScope.Minimal"/> scope when low VM is detected to improve performance.
    /// </summary>
    public static bool LowMemoryForcedMinimalBackgroundAnalysis = false;

    /// <summary>
    /// <para>Gets the effective background analysis scope for the current solution.</para>
    ///
    /// <para>Gets the solution-specific analysis scope set through
    /// <see cref="SolutionBackgroundAnalysisScopeOption"/>, or the default analysis scope if no solution-specific
    /// scope is set.</para>
    /// </summary>
    public static BackgroundAnalysisScope GetBackgroundAnalysisScope(this IGlobalOptionService globalOptions, string language)
    {
        if (LowMemoryForcedMinimalBackgroundAnalysis)
        {
            return BackgroundAnalysisScope.Minimal;
        }

        return globalOptions.GetOption(SolutionBackgroundAnalysisScopeOption) ??
               globalOptions.GetOption(BackgroundAnalysisScopeOption, language);
    }

    /// <summary>
    /// Returns true if full solution analysis is enabled for the given
    /// <paramref name="analyzer"/> through options for the given <paramref name="language"/>.
    /// </summary>
    public static bool IsFullSolutionAnalysisEnabled(this DiagnosticAnalyzer analyzer, IGlobalOptionService globalOptions, string language)
    {
        if (analyzer.IsCompilerAnalyzer())
        {
            return globalOptions.GetOption(CompilerDiagnosticsScopeOption, language) == CompilerDiagnosticsScope.FullSolution;
        }

        return GetBackgroundAnalysisScope(globalOptions, language) == BackgroundAnalysisScope.FullSolution;
    }

    /// <summary>
    /// Returns true if the entire solution will be analyzed in the background
    /// to compute up-to-date diagnostics for the error list.
    /// Note that the background analysis scope for compiler diagnostics and
    /// analyzers can be different. If you want to fetch individual values for
    /// whether or not full solution analysis is enabled for compiler diagnostics
    /// and analyzers, use the other overload
    /// <see cref="IsFullSolutionAnalysisEnabled(IGlobalOptionService, string, out bool, out bool)"/>.
    /// </summary>
    public static bool IsFullSolutionAnalysisEnabled(
        this IGlobalOptionService globalOptions,
        string language)
        => globalOptions.IsFullSolutionAnalysisEnabled(language, out _, out _);

    /// <summary>
    /// Returns true if the entire solution will be analyzed in the background
    /// to compute up-to-date diagnostics for the error list.
    /// Note that the background analysis scope for compiler diagnostics and
    /// analyzers can be different. Full analysis is enabled if either
    /// <paramref name="compilerFullSolutionAnalysisEnabled"/> is true or
    /// <paramref name="analyzersFullSolutionAnalysisEnabled"/> is true.
    /// Full analysis is disabled only if both these flags are false.
    /// If you do not care about the individual full solution analysis values
    /// for compiler diagnostics and analyzers, use the other overload
    /// <see cref="IsFullSolutionAnalysisEnabled(IGlobalOptionService, string)"/>.
    /// </summary>
    /// <param name="globalOptions">Global options.</param>
    /// <param name="language">
    /// Language of the projects in the solution to analyze.
    /// </param>
    /// <param name="compilerFullSolutionAnalysisEnabled">
    /// Indicates if the compiler diagnostics need to be computed for the entire solution.
    /// </param>
    /// <param name="analyzersFullSolutionAnalysisEnabled">
    /// Indicates if analyzer diagnostics need to be computed for the entire solution.
    /// </param>
    public static bool IsFullSolutionAnalysisEnabled(
        this IGlobalOptionService globalOptions,
        string language,
        out bool compilerFullSolutionAnalysisEnabled,
        out bool analyzersFullSolutionAnalysisEnabled)
    {
        compilerFullSolutionAnalysisEnabled = globalOptions.GetOption(CompilerDiagnosticsScopeOption, language) == CompilerDiagnosticsScope.FullSolution;
        analyzersFullSolutionAnalysisEnabled = GetBackgroundAnalysisScope(globalOptions, language) == BackgroundAnalysisScope.FullSolution;
        return compilerFullSolutionAnalysisEnabled || analyzersFullSolutionAnalysisEnabled;
    }

    /// <summary>
    /// Returns true if background analysis is completely disabled for
    /// both compiler diagnostics and analyzer diagnostics, i.e. the user
    /// does not want to see squiggles or error list entries for any diagnostics.
    /// </summary>
    public static bool IsAnalysisDisabled(
        this IGlobalOptionService globalOptions,
        string language)
    {
        var compilerDiagnosticsDisabled = globalOptions.GetOption(CompilerDiagnosticsScopeOption, language) == CompilerDiagnosticsScope.None;
        var analyzersDisabled = GetBackgroundAnalysisScope(globalOptions, language) == BackgroundAnalysisScope.None;
        return compilerDiagnosticsDisabled && analyzersDisabled;
    }
}
