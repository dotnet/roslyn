// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that the option should be stored into a roamed profile across machines.
    /// </summary>
    internal sealed class RoamingProfileStorageLocation : OptionStorageLocation2
    {
        private readonly Func<string, string> _keyNameFromLanguageName;

        public string GetKeyNameForLanguage(string languageName)
        {
            var unsubstitutedKeyName = _keyNameFromLanguageName(languageName);

            if (languageName == null)
            {
                return unsubstitutedKeyName;
            }
            else
            {
                var substituteLanguageName = languageName == LanguageNames.CSharp ? "CSharp" :
                                                languageName == LanguageNames.VisualBasic ? "VisualBasic" :
                                                languageName;

                return unsubstitutedKeyName.Replace("%LANGUAGE%", substituteLanguageName);
            }
        }

        public RoamingProfileStorageLocation(string keyName)
            => _keyNameFromLanguageName = _ => keyName;

        /// <summary>
        /// Creates a <see cref="RoamingProfileStorageLocation"/> that has different key names for different languages.
        /// </summary>
        /// <param name="keyNameFromLanguageName">A function that maps from a <see cref="LanguageNames"/> value to the key name.</param>
        public RoamingProfileStorageLocation(Func<string, string> keyNameFromLanguageName)
            => _keyNameFromLanguageName = keyNameFromLanguageName;
    }
}
