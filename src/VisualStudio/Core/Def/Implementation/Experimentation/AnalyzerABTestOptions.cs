// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Experimentation
{
    internal static class AnalyzerABTestOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\Analyzers\AB\Vsix\";

        [ExportOption]
        public static readonly Option<long> LastDateTimeUsedSuggestionAction = new Option<long>(nameof(AnalyzerABTestOptions),
            nameof(LastDateTimeUsedSuggestionAction), defaultValue: DateTime.MinValue.ToBinary(),
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(LastDateTimeUsedSuggestionAction)));

        [ExportOption]
        public static readonly Option<uint> UsedSuggestedActionCount = new Option<uint>(nameof(AnalyzerABTestOptions), nameof(UsedSuggestedActionCount),
            defaultValue: 0, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(UsedSuggestedActionCount)));

        [ExportOption]
        public static readonly Option<bool> NeverShowAgain = new Option<bool>(nameof(AnalyzerABTestOptions), nameof(NeverShowAgain),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(NeverShowAgain)));

        [ExportOption]
        public static readonly Option<bool> HasMetCandidacyRequirements = new Option<bool>(nameof(AnalyzerABTestOptions), nameof(HasMetCandidacyRequirements),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(HasMetCandidacyRequirements)));

        [ExportOption]
        public static readonly Option<long> LastDateTimeInfoBarShown = new Option<long>(nameof(AnalyzerABTestOptions), nameof(LastDateTimeInfoBarShown),
            defaultValue: DateTime.MinValue.ToBinary(),
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(LastDateTimeInfoBarShown)));
    }
}
