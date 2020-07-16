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
    internal static class StorageOptions
    {
        internal const string LocalRegistryPath = @"Roslyn\Internal\OnOff\Features\";

        public const string OptionName = "FeatureManager/Storage";

        public static readonly Option<StorageDatabase> Database = new Option<StorageDatabase>(
            OptionName, nameof(Database), defaultValue: StorageDatabase.SQLite);

        public static readonly Option<bool> SQLiteInMemoryWriteCache = new Option<bool>(
            OptionName, nameof(SQLiteInMemoryWriteCache), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(SQLiteInMemoryWriteCache)));
    }

    [ExportOptionProvider, Shared]
    internal class RemoteHostOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteHostOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            StorageOptions.Database,
            StorageOptions.SQLiteInMemoryWriteCache);
    }
}
