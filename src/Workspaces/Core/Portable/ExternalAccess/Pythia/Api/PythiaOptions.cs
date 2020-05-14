// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
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
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PythiaOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; }
            = ImmutableArray.Create<IOption>(
                PythiaOptions.ShowDebugInfo,
                PythiaOptions.RemoveRecommendationLimit);
    }
}
