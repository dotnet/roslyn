// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaOptions
    {
        public const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        public static readonly Option2<bool> ShowDebugInfo = new Option2<bool>(
            "InternalFeatureOnOffOptions", nameof(ShowDebugInfo), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(ShowDebugInfo)));

        public static readonly Option2<bool> RemoveRecommendationLimit = new Option2<bool>(
            "InternalFeatureOnOffOptions", nameof(RemoveRecommendationLimit), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(RemoveRecommendationLimit)));
    }

    [ExportOptionProvider, Shared]
    internal class PythiaOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public PythiaOptionsProvider()
        {
        }

        public ImmutableArray<Options.IOption> Options { get; }
            = ImmutableArray.Create<Options.IOption>(
                PythiaOptions.ShowDebugInfo,
                PythiaOptions.RemoveRecommendationLimit);
    }
}
