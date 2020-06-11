// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Host
{
    internal static class PersistentStorageOptions
    {
        public const string OptionName = "FeatureManager/Persistence";

        public static readonly Option<bool> Enabled = new Option<bool>(OptionName, "Enabled", defaultValue: true);
    }
}
