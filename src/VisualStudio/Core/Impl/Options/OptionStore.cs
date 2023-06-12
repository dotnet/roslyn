// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// Stores values of options read from global options and values set to these options.
    /// Not thread safe.
    /// </summary>
    internal sealed class OptionStore : IOptionsReader
    {
        public readonly IGlobalOptionService GlobalOptions;

        public event EventHandler<OptionKey2>? OptionChanged;

        /// <summary>
        /// Cached values read from global options.
        /// </summary>
        private ImmutableDictionary<OptionKey2, object?> _globalValues;

        /// <summary>
        /// Updated values.
        /// </summary>
        private ImmutableDictionary<OptionKey2, object?> _updatedValues;

        public OptionStore(IGlobalOptionService globalOptions)
        {
            GlobalOptions = globalOptions;

            _globalValues = ImmutableDictionary<OptionKey2, object?>.Empty;
            _updatedValues = ImmutableDictionary<OptionKey2, object?>.Empty;
        }

        public void Clear()
        {
            _globalValues = ImmutableDictionary<OptionKey2, object?>.Empty;
            _updatedValues = ImmutableDictionary<OptionKey2, object?>.Empty;
        }

        public ImmutableArray<(OptionKey2 key, object? oldValue, object? newValue)> GetChangedOptions()
            => _updatedValues.SelectAsArray(entry => (entry.Key, _globalValues[entry.Key], entry.Value));

        bool IOptionsReader.TryGetOption<T>(OptionKey2 optionKey, out T value)
        {
            value = (T)GetOption(optionKey)!;
            return true;
        }

        public T GetOption<T>(Option2<T> option)
            => (T)GetOption(new OptionKey2(option))!;

        public T GetOption<T>(PerLanguageOption2<T> option, string language)
            => (T)GetOption(new OptionKey2(option, language))!;

        public T GetOption<T>(IOption2 option, string? language)
        {
            Debug.Assert(option.IsPerLanguage == language is not null);
            return (T)GetOption(new OptionKey2(option, language))!;
        }

        private object? GetOption(OptionKey2 optionKey)
        {
            if (_updatedValues.TryGetValue(optionKey, out var value))
            {
                return value;
            }

            if (_globalValues.TryGetValue(optionKey, out value))
            {
                return value;
            }

            value = GlobalOptions.GetOption<object?>(optionKey);
            _globalValues = _globalValues.Add(optionKey, value);
            return value;
        }

        public void SetOption<T>(Option2<T> option, T value)
            => SetOption(new OptionKey2(option), value);

        public void SetOption<T>(PerLanguageOption2<T> option, string language, T value)
            => SetOption(new OptionKey2(option, language), value);

        public void SetOption(IOption2 option, string? language, object? value)
        {
            Debug.Assert(option.IsPerLanguage == language is not null);
            SetOption(new OptionKey2(option, language), value);
        }

        private void SetOption(OptionKey2 optionKey, object? value)
        {
            var currentValue = GetOption(optionKey);
            if (!Equals(value, currentValue))
            {
                _updatedValues = _updatedValues.SetItem(optionKey, value);
                OptionChanged?.Invoke(this, optionKey);
            }
        }
    }
}
