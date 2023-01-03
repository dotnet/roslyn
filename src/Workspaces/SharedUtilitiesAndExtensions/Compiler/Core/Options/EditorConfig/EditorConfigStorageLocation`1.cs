// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that an option should be read from an .editorconfig file.
    /// </summary>
    internal sealed class EditorConfigStorageLocation<T> : OptionStorageLocation2, IEditorConfigStorageLocation2
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

        public bool TryGetOption(StructuredAnalyzerConfigOptions options, Type type, out object? result)
        {
            if (options.TryGetValue(KeyName, out var value))
            {
                var ret = TryGetOption(value, out var typedResult);
                result = typedResult;
                return ret;
            }

            result = null;
            return false;
        }

        internal bool TryGetOption(string value, [MaybeNullWhen(false)] out T result)
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

        string IEditorConfigStorageLocation2.GetEditorConfigStringValue(object? value)
        {
            T typedValue;
            if (value is ICodeStyleOption codeStyleOption)
            {
                typedValue = (T)codeStyleOption.AsCodeStyleOption<T>();
            }
            else
            {
                typedValue = (T)value!;
            }

            return GetEditorConfigStringValue(typedValue);
        }

#if !CODE_STYLE
        public string GetEditorConfigStringValue(OptionKey optionKey, OptionSet optionSet)
        {
            if (_getValueFromOptionSet != null)
            {
                var editorConfigStringForValue = _serializeValue(_getValueFromOptionSet(optionSet));
                Contract.ThrowIfTrue(RoslynString.IsNullOrEmpty(editorConfigStringForValue));
                return editorConfigStringForValue;
            }

            return GetEditorConfigStringValue(optionSet.GetOption<T>(optionKey));
        }
#endif
    }
}
