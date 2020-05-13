// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Specifies that an option should be read from an .editorconfig file.
    /// </summary>
    internal sealed class EditorConfigStorageLocation<T> : OptionStorageLocation2, IEditorConfigStorageLocation2
    {
        public string KeyName { get; }

        private readonly Func<string, Type, Optional<T>> _parseValue;
        private readonly Func<T, OptionSet, string?> _getEditorConfigStringForValue;

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

        public bool TryGetOption(IReadOnlyDictionary<string, string?> rawOptions, Type type, out object? result)
        {
            if (rawOptions.TryGetValue(KeyName, out var value)
                && value is object)
            {
                var ret = TryGetOption(value, type, out var typedResult);
                result = typedResult;
                return ret;
            }

            result = null;
            return false;
        }

        internal bool TryGetOption(string value, Type type, [MaybeNullWhen(false)] out T result)
        {
            var optionalValue = _parseValue(value, type);
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

        /// <summary>
        /// Gets the editorconfig string representation for this storage location.
        /// </summary>
        public string GetEditorConfigStringValue(T value, OptionSet optionSet)
        {
            var editorConfigStringForValue = _getEditorConfigStringForValue(value, optionSet);
            RoslynDebug.Assert(!RoslynString.IsNullOrEmpty(editorConfigStringForValue));
            Debug.Assert(editorConfigStringForValue.All(ch => !(char.IsWhiteSpace(ch) || char.IsUpper(ch))));
            return editorConfigStringForValue;
        }

        string IEditorConfigStorageLocation2.GetEditorConfigString(object? value, OptionSet optionSet)
            => $"{KeyName} = {((IEditorConfigStorageLocation2)this).GetEditorConfigStringValue(value, optionSet)}";

        string IEditorConfigStorageLocation2.GetEditorConfigStringValue(object? value, OptionSet optionSet)
        {
            T typedValue;
            if (value is ICodeStyleOption codeStyleOption)
            {
                typedValue = (T)codeStyleOption.AsCodeStyleOption<T>();
            }
            else
            {
                typedValue = (T)value;
            }

            return GetEditorConfigStringValue(typedValue!, optionSet);
        }
    }
}
