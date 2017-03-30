// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Storage
{
    internal static class StorageOptions
    {
        public const string OptionName = "FeatureManager/Storage";

        public static readonly Option<StorageDatabase> Database = new Option<StorageDatabase>(
            OptionName, nameof(Database), defaultValue: StorageDatabase.Esent);
    }
}
