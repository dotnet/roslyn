// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers
{
    internal static class FxCopAnalyzersInstallOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\FxCopAnalyzers\";
        private const string LocalRegistryPath_CodeAnalysis2017 = @"Roslyn\Internal\Analyzers\AB\Vsix\";

        public static readonly Option2<long> LastDateTimeUsedSuggestionAction = new Option2<long>(nameof(FxCopAnalyzersInstallOptions),
            nameof(LastDateTimeUsedSuggestionAction), defaultValue: DateTime.MinValue.ToBinary(),
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(LastDateTimeUsedSuggestionAction)));

        public static readonly Option2<int> UsedSuggestedActionCount = new Option2<int>(nameof(FxCopAnalyzersInstallOptions), nameof(UsedSuggestedActionCount),
            defaultValue: 0, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(UsedSuggestedActionCount)));

        public static readonly Option2<bool> NeverShowAgain_CodeAnalysis2017 = new Option2<bool>(@"AnalyzerABTestOptions", @"NeverShowAgain",
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath_CodeAnalysis2017 + @"NeverShowAgain"));

        public static readonly Option2<bool> NeverShowAgain = new Option2<bool>(nameof(FxCopAnalyzersInstallOptions), nameof(NeverShowAgain),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath_CodeAnalysis2017 + nameof(NeverShowAgain)));

        public static readonly Option2<bool> HasMetCandidacyRequirements = new Option2<bool>(nameof(FxCopAnalyzersInstallOptions), nameof(HasMetCandidacyRequirements),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(HasMetCandidacyRequirements)));

        public static readonly Option2<long> LastDateTimeInfoBarShown = new Option2<long>(nameof(FxCopAnalyzersInstallOptions), nameof(LastDateTimeInfoBarShown),
            defaultValue: DateTime.MinValue.ToBinary(),
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(LastDateTimeInfoBarShown)));

        public static readonly Option2<bool> VsixInstalled = new Option2<bool>(nameof(FxCopAnalyzersInstallOptions),
            nameof(VsixInstalled), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(VsixInstalled)));
    }
}
