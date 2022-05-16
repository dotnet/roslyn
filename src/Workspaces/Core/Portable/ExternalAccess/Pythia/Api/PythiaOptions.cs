// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    [Obsolete("Use PythiaGlobalOptions instead")]
    internal static class PythiaOptions
    {
        public const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        public static readonly Option<bool> ShowDebugInfo = new(
            "InternalFeatureOnOffOptions", nameof(ShowDebugInfo), defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(ShowDebugInfo)));

        public static readonly Option<bool> RemoveRecommendationLimit = new(
            "InternalFeatureOnOffOptions", nameof(RemoveRecommendationLimit), defaultValue: false,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(RemoveRecommendationLimit)));
    }

    [Obsolete("Use PythiaGlobalOptions instead")]
    [ExportSolutionOptionProvider, Shared]
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
