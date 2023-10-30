// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CodeAnalysisSuggestions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

[Export(typeof(ISuggestedActionCallback))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CodeAnalysisSuggestionsSuggestedActionCallback(IThreadingContext threadingContext, IGlobalOptionService globalOptions)
    : ForegroundThreadAffinitizedObject(threadingContext), ISuggestedActionCallback
{
    private static readonly ImmutableHashSet<string> s_codeQualityAnalyzerAndFixerAssemblyNames = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "Microsoft.CodeAnalysis.NetAnalyzers",
        "Microsoft.CodeAnalysis.CSharp.NetAnalyzers",
        "Microsoft.CodeAnalysis.VisualBasic.NetAnalyzers");

    private static readonly ImmutableHashSet<string> s_codeStyleAnalyzerAndFixerAssemblyNames = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "Microsoft.CodeAnalysis.Features",
        "Microsoft.CodeAnalysis.CSharp.Features",
        "Microsoft.CodeAnalysis.VisualBasic.Features",
        "Microsoft.CodeAnalysis.CodeStyle",
        "Microsoft.CodeAnalysis.CSharp.CodeStyle",
        "Microsoft.CodeAnalysis.VisualBasic.CodeStyle",
        "Microsoft.CodeAnalysis.CodeStyle.Fixes",
        "Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes",
        "Microsoft.CodeAnalysis.VisualBasic.CodeStyle.Fixes");

    private readonly IGlobalOptionService _globalOptions = globalOptions;

    public void OnSuggestedActionExecuted(SuggestedAction action)
    {
        // We'll need to be on the UI thread for the operations.
        AssertIsForeground();

        // If the user has disabled the feature, then we bail out immediately.
        if (!_globalOptions.GetOption(CodeAnalysisSuggestionsOptionsStorage.ShowCodeAnalysisSuggestionsInLightbulb))
        {
            return;
        }

        // Update option values for first party code quality and code style fixes.
        var assemblyName = action.Provider.GetType().Assembly.GetName().Name;
        UpdateOptions(_globalOptions, assemblyName, codeQuality: true);
        UpdateOptions(_globalOptions, assemblyName, codeQuality: false);
        return;

        static void UpdateOptions(IGlobalOptionService options, string assemblyName, bool codeQuality)
        {
            var isFirstPartyFix = codeQuality
                ? s_codeQualityAnalyzerAndFixerAssemblyNames.Contains(assemblyName)
                : s_codeStyleAnalyzerAndFixerAssemblyNames.Contains(assemblyName);
            if (!isFirstPartyFix)
                return;

            // We want to track if the user has triggered a code quality/code style suggested action on 3 separate days.
            // We have options 'HasMetCandidacyRequirementsForXXX' for code quality/code style to track if the user
            // has already met this requirement, and hence is a candidate.
            // If not, we account for the current suggestion action invocation and update the invocation count and datetime.

            // If the user has already met candidacy conditions, then we have nothing to update.
            var hasMetCandidacyOption = codeQuality
                ? CodeAnalysisSuggestionsOptionsStorage.HasMetCandidacyRequirementsForCodeQuality
                : CodeAnalysisSuggestionsOptionsStorage.HasMetCandidacyRequirementsForCodeStyle;
            if (options.GetOption(hasMetCandidacyOption))
                return;

            // We store in UTC to avoid any timezone offset weirdness
            var lastUsedDateTimeOption = codeQuality
                ? CodeAnalysisSuggestionsOptionsStorage.LastDateTimeUsedCodeQualityFix
                : CodeAnalysisSuggestionsOptionsStorage.LastDateTimeUsedCodeStyleFix;
            var lastTriggeredTimeBinary = options.GetOption(lastUsedDateTimeOption);

            // Check if this is the first suggested action invoked on this day.
            var lastTriggeredTime = DateTime.FromBinary(lastTriggeredTimeBinary);
            var currentTime = DateTime.UtcNow;
            var span = currentTime - lastTriggeredTime;
            if (span.TotalDays < 1)
                return;

            options.SetGlobalOption(lastUsedDateTimeOption, currentTime.ToBinary());

            // Update the invoked fix count.
            var usedCountOption = codeQuality
                ? CodeAnalysisSuggestionsOptionsStorage.InvokedCodeQualityFixCount
                : CodeAnalysisSuggestionsOptionsStorage.InvokedCodeStyleFixCount;
            var usageCount = options.GetOption(usedCountOption);
            options.SetGlobalOption(usedCountOption, ++usageCount);

            // Mark candidate if user has invoked the fix 3 times.
            if (usageCount >= 3)
                options.SetGlobalOption(hasMetCandidacyOption, true);
        }
    }
}
