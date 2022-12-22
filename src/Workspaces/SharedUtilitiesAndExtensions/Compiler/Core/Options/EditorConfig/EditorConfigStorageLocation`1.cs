// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that an option should be read from an .editorconfig file.
    /// </summary>
    internal sealed class EditorConfigStorageLocation<T> : OptionStorageLocation2, IEditorConfigStorageLocation
    {
        public string KeyName { get; }

        private readonly Func<string, Optional<T>> _parseValue;
        private readonly Func<T, string> _serializeValue;

#if !CODE_STYLE
        private readonly Func<OptionSet, T>? _getValueFromOptionSet;

        public EditorConfigStorageLocation(
            string keyName,
            Func<string, Optional<T>> parseValue,
            Func<T, string> serializeValue,
            Func<OptionSet, T>? getValueFromOptionSet)
            : this(keyName, parseValue, serializeValue)
        {
            _getValueFromOptionSet = getValueFromOptionSet;
        }
#endif
        public EditorConfigStorageLocation(
            string keyName,
            Func<string, Optional<T>> parseValue,
            Func<T, string> serializeValue)
        {
            KeyName = keyName;
            _parseValue = parseValue;
            _serializeValue = serializeValue;
        }

        bool IEditorConfigStorageLocation.TryParseValue(string value, out object? result)
        {
            if (TryParseValue(value, out var typedResult))
            {
                result = typedResult;
                return true;
            }

            result = null;
            return false;
        }

        internal bool TryParseValue(string value, [MaybeNullWhen(false)] out T result)
        {
            var optionalValue = _parseValue(value);
            if (optionalValue.HasValue)
            {
                result = optionalValue.Value;
                return result != null;
            }
            else
            {
                result = default!;
                return false;
            }
        }

        public string GetEditorConfigStringValue(T value)
        {
            var editorConfigStringForValue = _serializeValue(value);
            Contract.ThrowIfTrue(RoslynString.IsNullOrEmpty(editorConfigStringForValue));
            return editorConfigStringForValue;
        }

        string IEditorConfigStorageLocation.GetEditorConfigStringValue(object? value)
            => GetEditorConfigStringValue((T)value!);

#if !CODE_STYLE
        public string GetEditorConfigStringValue(OptionKey optionKey, OptionSet optionSet)
        {
            if (_getValueFromOptionSet != null)
            {
                var editorConfigStringForValue = _serializeValue(_getValueFromOptionSet(optionSet));
                Contract.ThrowIfTrue(RoslynString.IsNullOrEmpty(editorConfigStringForValue));
                return editorConfigStringForValue;
            }

            var publicValue = optionSet.GetOption<object?>(optionKey);
            var internalValue = (publicValue is ICodeStyleOption codeStyleOption) ? codeStyleOption.AsCodeStyleOption<T>() : publicValue;

            return GetEditorConfigStringValue((T)internalValue!);
        }
#endif
    }
}
