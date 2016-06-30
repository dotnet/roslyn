using System;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that the option should be persisted into a roamed profile across machines.
    /// </summary>
    internal sealed class RoamingProfilePersistence : OptionPersistence
    {
        private readonly Func<string, string> _keyNameFromLanguageName;

        public string GetKeyNameForLanguage(string languageName)
        {
            string unsubstitutedKeyName = _keyNameFromLanguageName(languageName);

            if (languageName == null)
            {
                return unsubstitutedKeyName;
            }
            else
            {
                string substituteLanguageName = languageName == LanguageNames.CSharp ? "CSharp" :
                                                languageName == LanguageNames.VisualBasic ? "VisualBasic" :
                                                languageName;

                return unsubstitutedKeyName.Replace("%LANGUAGE%", substituteLanguageName);
            }
        }

        public RoamingProfilePersistence(string keyName)
        {
            _keyNameFromLanguageName = _ => keyName;
        }

        /// <summary>
        /// Creates a <see cref="RoamingProfilePersistence"/> that has different key names for different languages.
        /// </summary>
        /// <param name="keyNameFromLanguageName">A function that maps from a <see cref="LanguageNames"/> value to the key name.</param>
        public RoamingProfilePersistence(Func<string, string> keyNameFromLanguageName)
        {
            _keyNameFromLanguageName = keyNameFromLanguageName;
        }
    }
}
