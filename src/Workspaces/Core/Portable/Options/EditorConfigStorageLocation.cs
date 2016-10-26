// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that an option should be read from an .editorconfig file.
    /// </summary>
    internal sealed class EditorConfigStorageLocation : OptionStorageLocation
    {
        public string KeyName { get; }

        private Func<string, Type, object> _parseValue;

        public object ParseValue(string s, Type type) => _parseValue(s, type);

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
    }
}
