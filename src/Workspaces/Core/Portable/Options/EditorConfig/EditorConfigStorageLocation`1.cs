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
    internal sealed class EditorConfigStorageLocation<T> : OptionStorageLocation, IEditorConfigStorageLocation2
    {
        public string KeyName { get; }

        private readonly Func<string, Type, Optional<T>> _parseValue;
        private readonly Func<T, OptionSet, string> _getEditorConfigStringForValue;

        public EditorConfigStorageLocation(string keyName, Func<string, Optional<T>> parseValue, Func<T, string> getEditorConfigStringForValue)
            : this(keyName, parseValue, (value, optionSet) => getEditorConfigStringForValue(value))
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

        public bool TryGetOption(IReadOnlyDictionary<string, string> rawOptions, Type type, out object result)
        {
            if (rawOptions.TryGetValue(KeyName, out var value))
            {
                var ret = TryGetOption(value, type, out var typedResult);
                result = typedResult;
                return ret;
            }

            result = null;
            return false;
        }

        internal bool TryGetOption(string value, Type type, out T result)
        {
            var optionalValue = _parseValue(value, type);
            if (optionalValue.HasValue)
            {
                result = optionalValue.Value;
                return result != null;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Gets the editorconfig string representation for this storage location.
        /// </summary>
        public string GetEditorConfigString(T value, OptionSet optionSet)
        {
            var editorConfigStringForValue = _getEditorConfigStringForValue(value, optionSet);
            Debug.Assert(!string.IsNullOrEmpty(editorConfigStringForValue));
            Debug.Assert(editorConfigStringForValue.All(ch => !(char.IsWhiteSpace(ch) || char.IsUpper(ch))));
            return $"{KeyName} = {editorConfigStringForValue}";
        }

        string IEditorConfigStorageLocation2.GetEditorConfigString(object value, OptionSet optionSet)
            => GetEditorConfigString((T)value, optionSet);
    }
}
