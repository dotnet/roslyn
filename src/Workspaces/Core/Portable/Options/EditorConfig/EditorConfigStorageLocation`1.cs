// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that an option should be read from an .editorconfig file.
    /// </summary>
    internal sealed class EditorConfigStorageLocation<T> : OptionStorageLocation, IEditorConfigStorageLocationWithKey
    {
        public string KeyName { get; }

        private readonly Func<string, Type, Optional<T>> _parseValue;
        private readonly Func<T, OptionSet, string> _getEditorConfigStringForValue;

        public EditorConfigStorageLocation(string keyName, Func<string, Optional<T>> parseValue, Func<T, string> getEditorConfigStringForValue)
            : this (keyName, parseValue, (value, optionSet) => getEditorConfigStringForValue(value))
        {
            if (getEditorConfigStringForValue == null)
            {
                throw new ArgumentNullException(nameof(getEditorConfigStringForValue));
            }
        }

        public EditorConfigStorageLocation(string keyName, Func<string, Optional<T>> parseValue, Func<OptionSet, string> getEditorConfigStringForValue)
            : this(keyName, parseValue, (value, optionSet) => getEditorConfigStringForValue(optionSet))
        {
            if (getEditorConfigStringForValue == null)
            {
                throw new ArgumentNullException(nameof(getEditorConfigStringForValue));
            }
        }

        public EditorConfigStorageLocation(string keyName, Func<string, Optional<T>> parseValue, Func<T, OptionSet, string> getEditorConfigStringForValue)
        {
            if (parseValue == null)
            {
                throw new ArgumentNullException(nameof(parseValue));
            }

            KeyName = keyName ?? throw new ArgumentNullException(nameof(keyName));

            // If we're explicitly given a parsing function we can throw away the type when parsing
            _parseValue = (s, type) => parseValue(s);

            _getEditorConfigStringForValue = getEditorConfigStringForValue ?? throw new ArgumentNullException(nameof(getEditorConfigStringForValue));
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

        /// <summary>
        /// Gets the editorconfig string representation for the value corresponding to the <see cref="KeyName"/>.
        /// Note that for related editor config locations that share the same <see cref="KeyName"/>, the returned string will be identical.
        /// </summary>
        public string GetEditorConfigStringForValue(T value, OptionSet optionSet)
        {
            var editorConfigstring = _getEditorConfigStringForValue(value, optionSet);
            Debug.Assert(!string.IsNullOrEmpty(editorConfigstring));
            Debug.Assert(editorConfigstring.All(ch => !(char.IsWhiteSpace(ch) || char.IsUpper(ch))));
            return editorConfigstring;
        }

        string IEditorConfigStorageLocationWithKey.GetEditorConfigStringForValue(object value, OptionSet optionSet)
            => GetEditorConfigStringForValue((T)value, optionSet);
    }
}
