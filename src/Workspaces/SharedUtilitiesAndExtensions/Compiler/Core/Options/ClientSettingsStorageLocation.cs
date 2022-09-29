// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Specifies that the option is stored in setting storage of the client.
/// </summary>
/// <remarks>
/// TODO (https://github.com/dotnet/roslyn/issues/62683): The options that use this storage are global client options.
/// This storage should really be in the VS layer but currently option storage is coupled with option definition and thus the storage is needed here.
/// </remarks>
internal abstract class ClientSettingsStorageLocation : OptionStorageLocation2
{
    private readonly Func<string?, string> _keyNameFromLanguageName;

    public ClientSettingsStorageLocation(string keyName)
        => _keyNameFromLanguageName = _ => keyName;

    /// <summary>
    /// Creates a <see cref="ClientSettingsStorageLocation"/> that has different key names for different languages.
    /// </summary>
    /// <param name="keyNameFromLanguageName">A function that maps from a <see cref="LanguageNames"/> value to the key name.</param>
    public ClientSettingsStorageLocation(Func<string?, string> keyNameFromLanguageName)
        => _keyNameFromLanguageName = keyNameFromLanguageName;

    public abstract bool IsMachineLocal { get; }

    public string GetKeyNameForLanguage(string? languageName)
    {
        var keyName = _keyNameFromLanguageName(languageName);

        if (languageName != null)
        {
            keyName = keyName.Replace("%LANGUAGE%", languageName switch
            {
                LanguageNames.CSharp => "CSharp",
                LanguageNames.VisualBasic => "VisualBasic",
                _ => languageName // handles F#, TypeScript and Xaml
            });
        }

        return keyName;
    }
}
