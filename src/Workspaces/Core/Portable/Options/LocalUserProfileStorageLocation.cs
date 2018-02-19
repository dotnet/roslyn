// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that the option should be stored into the user's local registry hive.
    /// </summary>
    internal sealed class LocalUserProfileStorageLocation : OptionStorageLocation
    {
        public string KeyName { get; }

        public LocalUserProfileStorageLocation(string keyName)
        {
            KeyName = keyName;
        }
    }
}
