// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            foreach (var optionKey in newOptions.GetChangedOptions(oldOptions))
            {
                var oldValue = oldOptions.GetOption(optionKey);
                var currentValue = newOptions.GetOption(optionKey);

                Logger.Log(FunctionId.Run_Environment_Options, Create(optionKey, oldValue, currentValue));
            }
        }

        private static KeyValueLogMessage Create(OptionKey optionKey, object oldValue, object currentValue)
        {
            return KeyValueLogMessage.Create(m =>
            {
                m[Name] = optionKey.Option.Name;
                m[Language] = optionKey.Language ?? All;
                m[Change] = CreateOptionValue(oldValue, currentValue);
            });
        }

        private static string CreateOptionValue(object oldValue, object currentValue)
        {
            var oldString = GetOptionValue(oldValue);
            var newString = GetOptionValue(currentValue);

            return oldString + "->" + newString;
        }

        private static string GetOptionValue(object oldValue)
        {
            return oldValue == null ? "[null]" : oldValue.ToString();
        }
    }
}
