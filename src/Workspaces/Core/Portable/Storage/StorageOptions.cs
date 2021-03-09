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

        public static readonly Option<StorageDatabase> Database = new(
            OptionName, nameof(Database), defaultValue: StorageDatabase.SQLite,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(Database)));

        /// <summary>
        /// Option that can be set in certain scenarios (like tests) to indicate that the client expects the DB to
        /// succeed at all work and that it should not ever gracefully fall over.  Should not be set in normal host
        /// environments, where it is completely reasonable for things to fail (for example, if a client asks for a key
        /// that hasn't been stored yet).
        /// </summary>
        public static readonly Option<bool> DatabaseMustSucceed = new(OptionName, nameof(DatabaseMustSucceed), defaultValue: false);
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
            StorageOptions.DatabaseMustSucceed);
    }
}
