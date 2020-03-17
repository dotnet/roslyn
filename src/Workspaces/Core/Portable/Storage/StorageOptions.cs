// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Storage
{
    internal static class StorageOptions
    {
        public const string OptionName = "FeatureManager/Storage";

        public static readonly Option<StorageDatabase> Database = new Option<StorageDatabase>(
            OptionName, nameof(Database), defaultValue: StorageDatabase.SQLite);
    }
}
