// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Experimentation
{
    internal static class AnalyzerABTestOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\Analyzers\AB\Vsix\";

        public static readonly Option<long> LastDateTimeUsedSuggestionAction = new Option<long>(nameof(AnalyzerABTestOptions),
            nameof(LastDateTimeUsedSuggestionAction), defaultValue: DateTime.MinValue.ToBinary(),
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(LastDateTimeUsedSuggestionAction)));

        public static readonly Option<int> UsedSuggestedActionCount = new Option<int>(nameof(AnalyzerABTestOptions), nameof(UsedSuggestedActionCount),
            defaultValue: 0, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(UsedSuggestedActionCount)));

        public static readonly Option<bool> NeverShowAgain = new Option<bool>(nameof(AnalyzerABTestOptions), nameof(NeverShowAgain),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(NeverShowAgain)));

        public static readonly Option<bool> HasMetCandidacyRequirements = new Option<bool>(nameof(AnalyzerABTestOptions), nameof(HasMetCandidacyRequirements),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(HasMetCandidacyRequirements)));

        public static readonly Option<long> LastDateTimeInfoBarShown = new Option<long>(nameof(AnalyzerABTestOptions), nameof(LastDateTimeInfoBarShown),
            defaultValue: DateTime.MinValue.ToBinary(),
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(LastDateTimeInfoBarShown)));

        public static readonly Option<bool> VsixInstalled = new Option<bool>(nameof(AnalyzerABTestOptions),
            nameof(VsixInstalled), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(VsixInstalled)));

        public static readonly Option<bool> ParticipatedInExperiment = new Option<bool>(nameof(AnalyzerABTestOptions),
            nameof(ParticipatedInExperiment), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(ParticipatedInExperiment)));
    }
}
