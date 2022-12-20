// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Options
{
    public abstract partial class OptionSet
    {
        internal static readonly OptionSet Empty = new EmptyOptionSet();

        private static readonly ImmutableDictionary<string, AnalyzerConfigOptions> s_emptyAnalyzerConfigOptions =
            ImmutableDictionary.Create<string, AnalyzerConfigOptions>(StringComparer.Ordinal);

        /// <summary>
        /// Map from language name to the <see cref="AnalyzerConfigOptions"/> wrapper.
        /// </summary>
        private ImmutableDictionary<string, AnalyzerConfigOptions> _lazyAnalyzerConfigOptions = s_emptyAnalyzerConfigOptions;

        protected OptionSet()
        {
        }

        private protected abstract object? GetOptionCore(OptionKey optionKey);

        /// <summary>
        /// Gets the value of the option, or the default value if not otherwise set.
        /// </summary>
        public object? GetOption(OptionKey optionKey)
            => GetOptionCore(optionKey);

        /// <summary>
        /// Gets the value of the option, or the default value if not otherwise set.
        /// </summary>
        public T GetOption<T>(OptionKey optionKey)
            => (T)GetOptionCore(optionKey)!;

#pragma warning disable RS0030 // Do not used banned APIs: PerLanguageOption<T>
        /// <summary>
        /// Gets the value of the option, or the default value if not otherwise set.
        /// </summary>
        public T GetOption<T>(Option<T> option)
            => GetOption<T>(new OptionKey(option));

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public OptionSet WithChangedOption<T>(Option<T> option, T value)
            => WithChangedOption(new OptionKey(option), value);

        /// <summary>
        /// Gets the value of the option, or the default value if not otherwise set.
        /// </summary>
        public T GetOption<T>(PerLanguageOption<T> option, string? language)
            => GetOption<T>(new OptionKey(option, language));

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public OptionSet WithChangedOption<T>(PerLanguageOption<T> option, string? language, T value)
            => WithChangedOption(new OptionKey(option, language), value);
#pragma warning restore

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public abstract OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value);

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        internal OptionSet WithChangedOption<T>(PerLanguageOption2<T> option, string? language, T value)
            => WithChangedOption(new OptionKey(option, language), value);

        internal AnalyzerConfigOptions AsAnalyzerConfigOptions(IEditorConfigOptionMapping mapping, string language)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref _lazyAnalyzerConfigOptions,
                language,
                static (string language, (OptionSet self, IEditorConfigOptionMapping mapping) arg) => arg.self.CreateAnalyzerConfigOptions(arg.mapping, language),
                (this, mapping));
        }

        internal abstract IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet);

        private protected virtual AnalyzerConfigOptions CreateAnalyzerConfigOptions(IEditorConfigOptionMapping mapping, string language)
            => new AnalyzerConfigOptionsImpl(this, mapping, language);
    }
}
