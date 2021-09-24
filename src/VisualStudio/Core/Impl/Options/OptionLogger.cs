// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal static class OptionLogger
    {
        private const string Name = nameof(Name);
        private const string Language = nameof(Language);
        private const string Change = nameof(Change);
        private const string All = nameof(All);

        public static void Log(OptionSet oldOptions, OptionSet newOptions)
        {
            if (oldOptions is not SerializableOptionSet serializableOldOptions ||
                newOptions is not SerializableOptionSet serializableNewOptions)
            {
                return;
            }

            var oldMap = serializableOldOptions.OptionKeyToValue;
            var newMap = serializableNewOptions.OptionKeyToValue;

            foreach (var optionKey in oldMap.Keys.Union(newMap.Keys))
            {
                var oldValue = oldMap.TryGetValue(optionKey, out var v1) ? v1 : null;
                var newValue = newMap.TryGetValue(optionKey, out var v2) ? v2 : null;

                Logger.Log(FunctionId.Run_Environment_Options, Create(optionKey, oldValue, newValue));
            }
        }

        private static KeyValueLogMessage Create(OptionKey optionKey, object? oldValue, object? currentValue)
        {
            return KeyValueLogMessage.Create(m =>
            {
                m[Name] = optionKey.Option.Name;
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
