// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

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

        public override object GetOption(OptionKey optionKey)
        {
            return _backingOptionSet.GetOption(optionKey);
        }

        public T GetOption<T>(PerLanguageOption<T> option)
        {
            return _backingOptionSet.GetOption(option, _language);
        }

        public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object value)
        {
            return new DocumentOptionSet(_backingOptionSet.WithChangedOption(optionAndLanguage, value), _language);
        }

        /// <summary>
        /// Creates a new <see cref="DocumentOptionSet" /> that contains the changed value.
        /// </summary>
        public DocumentOptionSet WithChangedOption<T>(PerLanguageOption<T> option, T value)
        {
            return (DocumentOptionSet)WithChangedOption(option, _language, value);
        }

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
        {
            return _backingOptionSet.GetChangedOptions(optionSet);
        }
    }
}
