// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Specifies that the option should be stored into a roamed profile across machines.
/// </summary>
internal sealed class RoamingProfileStorageLocation : ClientSettingsStorageLocation
{
    public override bool IsMachineLocal => false;

    public RoamingProfileStorageLocation(string keyName)
        : base(keyName)
    {
    }

    /// <summary>
    /// Creates a <see cref="RoamingProfileStorageLocation"/> that has different key names for different languages.
    /// </summary>
    /// <param name="keyNameFromLanguageName">A function that maps from a <see cref="LanguageNames"/> value to the key name.</param>
    public RoamingProfileStorageLocation(Func<string?, string> keyNameFromLanguageName)
        : base(keyNameFromLanguageName)
    {
    }
}
