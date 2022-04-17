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

    public static bool IsFullSolutionAnalysisEnabled(this DiagnosticAnalyzer analyzer, IGlobalOptionService globalOptions, string language)
    {
        if (analyzer.IsCompilerAnalyzer())
        {
            return globalOptions.GetOption(CompilerDiagnosticsScopeOption, language) == CompilerDiagnosticsScope.FullSolution;
        }

        return GetBackgroundAnalysisScope(globalOptions, language) == BackgroundAnalysisScope.FullSolution;
    }

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

    public static bool IsAnalysisDisabled(
        this IGlobalOptionService globalOptions,
        string language,
        out bool compilerDiagnosticsDisabled,
        out bool analyzersDisabled)
    {
        compilerDiagnosticsDisabled = globalOptions.GetOption(CompilerDiagnosticsScopeOption, language) == CompilerDiagnosticsScope.None;
        analyzersDisabled = GetBackgroundAnalysisScope(globalOptions, language) == BackgroundAnalysisScope.None;
        return compilerDiagnosticsDisabled && analyzersDisabled;
    }
}
