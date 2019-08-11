// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers
{
    internal static class FxCopAnalyzersInstallOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\FxCopAnalyzers\";
        private const string LocalRegistryPath_CodeAnalysis2017 = @"Roslyn\Internal\Analyzers\AB\Vsix\";

        public static readonly Option<long> LastDateTimeUsedSuggestionAction = new Option<long>(nameof(FxCopAnalyzersInstallOptions),
            nameof(LastDateTimeUsedSuggestionAction), defaultValue: DateTime.MinValue.ToBinary(),
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(LastDateTimeUsedSuggestionAction)));

        public static readonly Option<int> UsedSuggestedActionCount = new Option<int>(nameof(FxCopAnalyzersInstallOptions), nameof(UsedSuggestedActionCount),
            defaultValue: 0, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(UsedSuggestedActionCount)));

        public static readonly Option<bool> NeverShowAgain_CodeAnalysis2017 = new Option<bool>(@"AnalyzerABTestOptions", @"NeverShowAgain",
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath_CodeAnalysis2017 + @"NeverShowAgain"));

        public static readonly Option<bool> NeverShowAgain = new Option<bool>(nameof(FxCopAnalyzersInstallOptions), nameof(NeverShowAgain),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath_CodeAnalysis2017 + nameof(NeverShowAgain)));

        public static readonly Option<bool> HasMetCandidacyRequirements = new Option<bool>(nameof(FxCopAnalyzersInstallOptions), nameof(HasMetCandidacyRequirements),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(HasMetCandidacyRequirements)));

        public static readonly Option<long> LastDateTimeInfoBarShown = new Option<long>(nameof(FxCopAnalyzersInstallOptions), nameof(LastDateTimeInfoBarShown),
            defaultValue: DateTime.MinValue.ToBinary(),
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(LastDateTimeInfoBarShown)));

        public static readonly Option<bool> VsixInstalled = new Option<bool>(nameof(FxCopAnalyzersInstallOptions),
            nameof(VsixInstalled), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(VsixInstalled)));
    }
}
