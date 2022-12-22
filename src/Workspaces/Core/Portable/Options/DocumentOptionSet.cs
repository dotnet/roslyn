// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: DocumentOptionSet, Option<T>, PerLanguageOption<T>

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// An <see cref="OptionSet"/> that comes from <see cref="Document.GetOptionsAsync(System.Threading.CancellationToken)"/>. It behaves just like a normal
    /// <see cref="OptionSet"/> but remembers which language the <see cref="Document"/> is, so you don't have to
    /// pass that information redundantly when calling <see cref="GetOption{T}(PerLanguageOption{T})"/>.
    /// </summary>
    public sealed class DocumentOptionSet : OptionSet
    {
        private readonly OptionSet _underlyingOptions;
        private readonly StructuredAnalyzerConfigOptions? _configOptions;
        private ImmutableDictionary<OptionKey, object?> _values;
        private readonly string _language;

        internal DocumentOptionSet(StructuredAnalyzerConfigOptions? configOptions, OptionSet underlyingOptions, string language)
            : this(configOptions, underlyingOptions, language, ImmutableDictionary<OptionKey, object?>.Empty)
        {
        }

        private DocumentOptionSet(StructuredAnalyzerConfigOptions? configOptions, OptionSet underlyingOptions, string language, ImmutableDictionary<OptionKey, object?> values)
        {
            _language = language;
            _configOptions = configOptions;
            _underlyingOptions = underlyingOptions;
            _values = values;
        }

        internal string Language => _language;

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowLocks = false)]
        private protected override object? GetOptionCore(OptionKey optionKey)
        {
            // If we already know the document specific value, we're done
            if (_values.TryGetValue(optionKey, out var value))
            {
                return value;
            }

            if (TryGetAnalyzerConfigOption(optionKey, out value))
            {
                // Cache and return
                return ImmutableInterlocked.GetOrAdd(ref _values, optionKey, value);
            }

            // We don't have a document specific value, so forward
            return _underlyingOptions.GetOption(optionKey);
        }

        private bool TryGetAnalyzerConfigOption(OptionKey optionKey, out object? value)
        {
            if (_configOptions == null)
            {
                value = null;
                return false;
            }

            if (optionKey.Option is not IOption2 internallyDefinedOption)
            {
                value = null;
                return false;
            }

            if (internallyDefinedOption.Type == typeof(NamingStylePreferences))
            {
                var preferences = _configOptions.GetNamingStylePreferences();
                value = preferences;
                return !preferences.IsEmpty;
            }

            if (!_configOptions.TryGetValue(internallyDefinedOption.OptionDefinition.ConfigName, out var stringValue))
            {
                value = null;
                return false;
            }

            var storage = (IEditorConfigStorageLocation)internallyDefinedOption.StorageLocations.Single();
            return storage.TryParseValue(stringValue, out value);
        }

        public T GetOption<T>(PerLanguageOption<T> option)
            => GetOption(option, _language);

        public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value)
            => new DocumentOptionSet(_configOptions, _underlyingOptions, _language, _values.SetItem(optionAndLanguage, value));

        /// <summary>
        /// Creates a new <see cref="DocumentOptionSet" /> that contains the changed value.
        /// </summary>
        public DocumentOptionSet WithChangedOption<T>(PerLanguageOption<T> option, T value)
            => (DocumentOptionSet)WithChangedOption(option, _language, value);

        private protected override AnalyzerConfigOptions CreateAnalyzerConfigOptions(IEditorConfigOptionMapping mapping, string? language)
        {
            Debug.Assert((language ?? _language) == _language, $"Use of a {nameof(DocumentOptionSet)} is not expected to differ from the language it was constructed with.");
            return base.CreateAnalyzerConfigOptions(mapping, language ?? _language);
        }

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
            => GetChangedOptions(optionSet);
    }
}
