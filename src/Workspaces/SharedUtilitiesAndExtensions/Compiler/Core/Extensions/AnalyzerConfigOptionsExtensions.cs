// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

#if CODE_STYLE
using TOption = Microsoft.CodeAnalysis.Options.IOption2;
#else
using TOption = Microsoft.CodeAnalysis.Options.IOption;
#endif

namespace Microsoft.CodeAnalysis
{
    internal static class AnalyzerConfigOptionsExtensions
    {
#if CODE_STYLE
        public static T GetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, PerLanguageOption2<T> option, string language)
        {
            // Language is not used for .editorconfig lookups
            _ = language;

            return GetOption(analyzerConfigOptions, option);
        }
#else
        public static T GetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, Options.Option<T> option)
            => GetOptionWithAssertOnFailure<T>(analyzerConfigOptions, option);

        public static T GetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, Options.PerLanguageOption<T> option)
            => GetOptionWithAssertOnFailure<T>(analyzerConfigOptions, option);
#endif

        public static T GetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, Option2<T> option)
            => GetOptionWithAssertOnFailure<T>(analyzerConfigOptions, option);

        public static T GetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, PerLanguageOption2<T> option)
            => GetOptionWithAssertOnFailure<T>(analyzerConfigOptions, option);

        private static T GetOptionWithAssertOnFailure<T>(AnalyzerConfigOptions analyzerConfigOptions, TOption option)
        {
            if (!TryGetEditorConfigOptionOrDefault(analyzerConfigOptions, option, out T value))
            {
                // There are couple of reasons this assert might fire:
                //  1. Attempting to access an option which does not have an IEditorConfigStorageLocation.
                //  2. Attempting to access an option which is not exposed from any option provider, i.e. IOptionProvider.Options.
                Debug.Fail("Failed to find a .editorconfig key for the option.");
                value = (T)option.DefaultValue!;
            }

            return value;
        }

        public static bool TryGetEditorConfigOptionOrDefault<T>(this AnalyzerConfigOptions analyzerConfigOptions, TOption option, out T value)
            => TryGetEditorConfigOption(analyzerConfigOptions, option, (T?)option.DefaultValue, out value!);

        public static bool TryGetEditorConfigOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, TOption option, [MaybeNullWhen(false)] out T value)
            => TryGetEditorConfigOption(analyzerConfigOptions, option, defaultValue: default, out value);

        public static T GetEditorConfigOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, TOption option, T defaultValue)
            => TryGetEditorConfigOption(analyzerConfigOptions, option, new Optional<T?>(defaultValue), out var value) ? value! : throw ExceptionUtilities.Unreachable;

        private static bool TryGetEditorConfigOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, TOption option, Optional<T?> defaultValue, out T? value)
        {
            var hasEditorConfigStorage = false;
            foreach (var storageLocation in option.StorageLocations)
            {
                // This code path will avoid allocating a Dictionary wrapper since we can get direct access to the KeyName.
                if (storageLocation is EditorConfigStorageLocation<T> editorConfigStorageLocation &&
                    analyzerConfigOptions.TryGetValue(editorConfigStorageLocation.KeyName, out var stringValue) &&
                    editorConfigStorageLocation.TryGetOption(stringValue, typeof(T), out value))
                {
                    return true;
                }

                if (storageLocation is not IEditorConfigStorageLocation configStorageLocation)
                {
                    continue;
                }

                // This option has .editorconfig storage defined, even if the current configuration does not provide a
                // value for it.
                hasEditorConfigStorage = true;
                if (configStorageLocation.TryGetOption(analyzerConfigOptions, option.Type, out var objectValue))
                {
                    value = (T?)objectValue;
                    return true;
                }
            }

            if (defaultValue.HasValue)
            {
                value = defaultValue.Value;
                return hasEditorConfigStorage;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
