// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Specifies that the option should be stored into local client settings storage.
/// </summary>
/// <remarks>
/// Unlike LocalUserRegistryOptionPersister, which accesses the registry directly this storage is managed by VS Settings component.
/// 
/// TODO (https://github.com/dotnet/roslyn/issues/62683): The options that use this storage are global client options. This storage should really be in the VS layer but currently 
/// option storage is coupled with option definition and thus the storage is needed here.
/// </remarks>
internal sealed class LocalClientSettingsStorageLocation : ClientSettingsStorageLocation
{
    public override bool IsMachineLocal => true;

    public LocalClientSettingsStorageLocation(string keyName)
        : base(keyName)
    {
    }

    /// <summary>
    /// Creates a <see cref="RoamingProfileStorageLocation"/> that has different key names for different languages.
    /// </summary>
    /// <param name="keyNameFromLanguageName">A function that maps from a <see cref="LanguageNames"/> value to the key name.</param>
    public LocalClientSettingsStorageLocation(Func<string?, string> keyNameFromLanguageName)
        : base(keyNameFromLanguageName)
    {
    }
}
