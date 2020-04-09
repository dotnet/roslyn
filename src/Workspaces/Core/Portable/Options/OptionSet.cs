// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Options
{
    public abstract partial class OptionSet
    {
        private const string NoLanguageSentinel = "\0";
        private static readonly ImmutableDictionary<string, AnalyzerConfigOptions> s_emptyAnalyzerConfigOptions =
            ImmutableDictionary.Create<string, AnalyzerConfigOptions>(StringComparer.Ordinal);

        /// <summary>
        /// Map from language name to the <see cref="AnalyzerConfigOptions"/> wrapper.
        /// </summary>
        private ImmutableDictionary<string, AnalyzerConfigOptions> _lazyAnalyzerConfigOptions = s_emptyAnalyzerConfigOptions;

        private protected abstract object? GetOptionCore(OptionKey optionKey);

        /// <summary>
        /// Gets the value of the option, or the default value if not otherwise set.
        /// </summary>
        public object? GetOption(OptionKey optionKey)
            => OptionsHelpers.GetPublicOption(optionKey, GetOptionCore);

        /// <summary>
        /// Gets the value of the option cast to type <typeparamref name="T"/>, or the default value if not otherwise set.
        /// </summary>
        public T GetOption<T>(OptionKey optionKey)
            => OptionsHelpers.GetOption<T>(optionKey, GetOptionCore);

        /// <summary>
        /// Gets the value of the option, or the default value if not otherwise set.
        /// </summary>
        internal object? GetOption(OptionKey2 optionKey)
            => OptionsHelpers.GetOption<object?>(optionKey, GetOptionCore);

        /// <summary>
        /// Gets the value of the option cast to type <typeparamref name="T"/>, or the default value if not otherwise set.
        /// </summary>
        internal T GetOption<T>(OptionKey2 optionKey)
            => OptionsHelpers.GetOption<T>(optionKey, GetOptionCore);

        /// <summary>
        /// Gets the value of the option, or the default value if not otherwise set.
        /// </summary>
        public T GetOption<T>(Option<T> option)
            => OptionsHelpers.GetOption(option, GetOptionCore);

        /// <summary>
        /// Gets the value of the option, or the default value if not otherwise set.
        /// </summary>
        internal T GetOption<T>(Option2<T> option)
            => OptionsHelpers.GetOption(option, GetOptionCore);

        /// <summary>
        /// Gets the value of the option, or the default value if not otherwise set.
        /// </summary>
        public T GetOption<T>(PerLanguageOption<T> option, string? language)
            => OptionsHelpers.GetOption(option, language, GetOptionCore);

        /// <summary>
        /// Gets the value of the option, or the default value if not otherwise set.
        /// </summary>
        internal T GetOption<T>(PerLanguageOption2<T> option, string? language)
            => OptionsHelpers.GetOption(option, language, GetOptionCore);

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public abstract OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value);

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        internal OptionSet WithChangedOption(OptionKey2 optionAndLanguage, object? value)
            => WithChangedOption((OptionKey)optionAndLanguage, value);

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public OptionSet WithChangedOption<T>(Option<T> option, T value)
            => WithChangedOption(new OptionKey(option), value);

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        internal OptionSet WithChangedOption<T>(Option2<T> option, T value)
            => WithChangedOption(new OptionKey(option), value);

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public OptionSet WithChangedOption<T>(PerLanguageOption<T> option, string? language, T value)
            => WithChangedOption(new OptionKey(option, language), value);

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        internal OptionSet WithChangedOption<T>(PerLanguageOption2<T> option, string? language, T value)
            => WithChangedOption(new OptionKey(option, language), value);

        internal AnalyzerConfigOptions AsAnalyzerConfigOptions(IOptionService optionService, string? language)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref _lazyAnalyzerConfigOptions,
                language ?? NoLanguageSentinel,
                (string language, (OptionSet self, IOptionService optionService) arg) => arg.self.CreateAnalyzerConfigOptions(arg.optionService, (object)language == NoLanguageSentinel ? null : language),
                (this, optionService));
        }

        internal abstract IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet);

        private protected virtual AnalyzerConfigOptions CreateAnalyzerConfigOptions(IOptionService optionService, string? language)
            => new AnalyzerConfigOptionsImpl(this, optionService, language);
    }
}
