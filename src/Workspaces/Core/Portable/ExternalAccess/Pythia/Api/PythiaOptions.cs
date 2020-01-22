// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaOptions
    {
        public const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        public static readonly Option<bool> ShowDebugInfo = new Option<bool>(
            "InternalFeatureOnOffOptions", nameof(ShowDebugInfo), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(ShowDebugInfo)));

        public static readonly Option<bool> RemoveRecommendationLimit = new Option<bool>(
            "InternalFeatureOnOffOptions", nameof(RemoveRecommendationLimit), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(RemoveRecommendationLimit)));
    }

    [ExportOptionProvider, Shared]
    internal class PythiaOptionsProvider : IOptionProvider
    {
        public ImmutableArray<IOption> Options { get; }
            = ImmutableArray.Create<IOption>(
                PythiaOptions.ShowDebugInfo,
                PythiaOptions.RemoveRecommendationLimit);
    }
}
