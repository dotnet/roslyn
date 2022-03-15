// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// An <see cref="OptionSet"/> that comes from <see cref="Document.GetOptionsAsync(System.Threading.CancellationToken)"/>. It behaves just like a normal
    /// <see cref="OptionSet"/> but remembers which language the <see cref="Document"/> is, so you don't have to
    /// pass that information redundantly when calling <see cref="GetOption{T}(PerLanguageOption{T})"/>.
    /// </summary>
    public sealed class DocumentOptionSet : OptionSet
    {
        private readonly OptionSet _backingOptionSet;
        private readonly string _language;

        internal DocumentOptionSet(OptionSet backingOptionSet, string language)
        {
            _backingOptionSet = backingOptionSet;
            _language = language;
        }

        internal string Language => _language;

        private protected override object? GetOptionCore(OptionKey optionKey)
            => _backingOptionSet.GetOption(optionKey);

        public T GetOption<T>(PerLanguageOption<T> option)
            => _backingOptionSet.GetOption(option, _language);

        internal T GetOption<T>(PerLanguageOption2<T> option)
            => _backingOptionSet.GetOption(option, _language);

        public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value)
            => new DocumentOptionSet(_backingOptionSet.WithChangedOption(optionAndLanguage, value), _language);

        /// <summary>
        /// Creates a new <see cref="DocumentOptionSet" /> that contains the changed value.
        /// </summary>
        public DocumentOptionSet WithChangedOption<T>(PerLanguageOption<T> option, T value)
            => (DocumentOptionSet)WithChangedOption(option, _language, value);

        /// <summary>
        /// Creates a new <see cref="DocumentOptionSet" /> that contains the changed value.
        /// </summary>
        internal DocumentOptionSet WithChangedOption<T>(PerLanguageOption2<T> option, T value)
            => (DocumentOptionSet)WithChangedOption(option, _language, value);

        private protected override AnalyzerConfigOptions CreateAnalyzerConfigOptions(IOptionService optionService, string? language)
        {
            Debug.Assert((language ?? _language) == _language, $"Use of a {nameof(DocumentOptionSet)} is not expected to differ from the language it was constructed with.");
            return _backingOptionSet.AsAnalyzerConfigOptions(optionService, language ?? _language);
        }

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
            => _backingOptionSet.GetChangedOptions(optionSet);
    }
}
