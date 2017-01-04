// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using static Microsoft.CodeAnalysis.CodeStyle.CodeStyleHelpers;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that an option should be read from an .editorconfig file.
    /// </summary>
    internal sealed class EditorConfigStorageLocation : OptionStorageLocation
    {
        public string KeyName { get; }

        private Func<string, Type, object> _parseValue;

        private Func<IReadOnlyDictionary<string, object>, Type, object> _parseDictionary;

        public bool TryParseReadonlyDictionary(IReadOnlyDictionary<string, object> allRawConventions, Type type, out object result)
        {
            if (_parseValue != null && KeyName != null)
            {
                if (allRawConventions.TryGetValue(KeyName, out object value))
                {
                    result = _parseValue(value.ToString(), type);
                    return true;
                }
            }
            else if (_parseDictionary != null)
            {
                result = _parseDictionary(allRawConventions, type);
                return true;
            }

            result = null;
            return false;
        }

        public EditorConfigStorageLocation(string keyName)
        {
            KeyName = keyName;

            _parseValue = (s, type) =>
            {
                if (type == typeof(int))
                {
                    return int.Parse(s);
                }
                else if (type == typeof(bool))
                {
                    return bool.Parse(s);
                }
                else if (type == typeof(CodeStyleOption<bool>))
                {
                    return ParseEditorConfigCodeStyleOption(s);
                }
                else
                {
                    throw new NotSupportedException(WorkspacesResources.Option_0_has_an_unsupported_type_to_use_with_1_You_should_specify_a_parsing_function);
                }
            };
        }

        public EditorConfigStorageLocation(string keyName, Func<string, object> parseValue)
        {
            KeyName = keyName;

            // If we're explicitly given a parsing function we can throw away the type when parsing
            _parseValue = (s, type) => parseValue(s);
        }

        public EditorConfigStorageLocation()
        {
            // If the user didn't pass a keyName assume we need to parse the entire dictionary
            _parseDictionary = (dictionary, type) =>
            {
                if (type == typeof(NamingStylePreferences))
                {
                    return EditorConfigNamingStyleParser.GetNamingStylesFromDictionary(dictionary);
                }
                else
                {
                    throw new NotSupportedException(WorkspacesResources.Option_0_has_an_unsupported_type_to_use_with_1_You_should_specify_a_parsing_function);
                }
            };
        }

        public EditorConfigStorageLocation(Func<IReadOnlyDictionary<string, object>, object> parseDictionary)
        {
            // If we're explicitly given a parsing function we can throw away the type when parsing
            _parseDictionary = (dictionary, type) => parseDictionary(dictionary);
        }
    }
}
