// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that the option should be stored into the user's local registry hive.
    /// </summary>
    internal sealed class LocalUserProfileStorageLocation : OptionStorageLocation2
    {
        public string KeyName { get; }

        public LocalUserProfileStorageLocation(string keyName)
            => KeyName = keyName;
    }
}
