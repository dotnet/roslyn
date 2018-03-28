// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that an option should be read from an .editorconfig file.
    /// </summary>
    internal sealed class EditorConfigStorageLocation<T> : OptionStorageLocation, IEditorConfigStorageLocation
    {
        public string KeyName { get; }

        private readonly Func<string, Type, Optional<T>> _parseValue;

        public EditorConfigStorageLocation(string keyName, Func<string, Optional<T>> parseValue)
        {
            if (parseValue == null)
            {
                throw new ArgumentNullException(nameof(parseValue));
            }

            KeyName = keyName ?? throw new ArgumentNullException(nameof(keyName));

            // If we're explicitly given a parsing function we can throw away the type when parsing
            _parseValue = (s, type) => parseValue(s);
        }

        public bool TryGetOption(object underlyingOption, IReadOnlyDictionary<string, object> allRawConventions, Type type, out object result)
        {
            if (allRawConventions.TryGetValue(KeyName, out object value))
            {
                var optionalValue = _parseValue(value.ToString(), type);
                if (optionalValue.HasValue)
                {
                    result = optionalValue.Value;
                }
                else
                {
                    result = null;
                }

                return result != null;
            }

            result = null;
            return false;
        }
    }
}
