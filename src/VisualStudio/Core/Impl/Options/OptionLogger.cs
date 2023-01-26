// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal static class OptionLogger
    {
        private const string ConfigName = nameof(ConfigName);
        private const string Language = nameof(Language);
        private const string Change = nameof(Change);
        private const string All = nameof(All);

        public static void Log(ImmutableArray<(OptionKey2 key, object? oldValue, object? newValue)> changedOptions)
        {
            foreach (var (optionKey, oldValue, newValue) in changedOptions)
            {
                Logger.Log(FunctionId.Run_Environment_Options, Create(optionKey, oldValue, newValue));
            }
        }

        private static KeyValueLogMessage Create(OptionKey2 optionKey, object? oldValue, object? currentValue)
        {
            return KeyValueLogMessage.Create(m =>
            {
                m[ConfigName] = optionKey.Option.Definition.ConfigName;
                m[Language] = optionKey.Language ?? All;
                m[Change] = CreateOptionValue(oldValue, currentValue);
            });
        }

        private static string CreateOptionValue(object? oldValue, object? currentValue)
        {
            var oldString = GetOptionValue(oldValue);
            var newString = GetOptionValue(currentValue);

            return oldString + "->" + newString;
        }

        private static string GetOptionValue(object? oldValue)
            => oldValue == null ? "[null]" : oldValue.ToString();
    }
}
