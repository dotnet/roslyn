// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Storage
{
    [ExportSolutionOptionProvider, Shared]
    internal sealed class StorageOptions : IOptionProvider
    {
        internal const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        private const string FeatureName = "FeatureManager/Storage";

        public static readonly Option2<StorageDatabase> Database = new(
            FeatureName, nameof(Database), defaultValue: StorageDatabase.SQLite,
            new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(Database)));

        public static readonly Option2<bool> CloudCacheFeatureFlag = new(
            FeatureName, nameof(CloudCacheFeatureFlag), defaultValue: false,
            new FeatureFlagStorageLocation("Roslyn.CloudCache3"));

        ImmutableArray<IOption> IOptionProvider.Options { get; } = ImmutableArray.Create<IOption>(
            Database,
            CloudCacheFeatureFlag);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StorageOptions()
        {
        }
    }
}
