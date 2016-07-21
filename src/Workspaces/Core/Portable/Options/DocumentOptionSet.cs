// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// An <see cref="OptionSet"/> that comes from <see cref="Document.Options"/>. It behaves just like a normal
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

        public override T GetOption<T>(Option<T> option)
        {
            return _backingOptionSet.GetOption(option);
        }

        public T GetOption<T>(PerLanguageOption<T> option)
        {
            return _backingOptionSet.GetOption(option, _language);
        }

        public override T GetOption<T>(PerLanguageOption<T> option, string language)
        {
            return _backingOptionSet.GetOption(option, language);
        }

        public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object value)
        {
            return new DocumentOptionSet(_backingOptionSet.WithChangedOption(optionAndLanguage, value), _language);
        }

        public override OptionSet WithChangedOption<T>(Option<T> option, T value)
        {
            return new DocumentOptionSet(_backingOptionSet.WithChangedOption(option, value), _language);
        }

        public override OptionSet WithChangedOption<T>(PerLanguageOption<T> option, string language, T value)
        {
            return new DocumentOptionSet(_backingOptionSet.WithChangedOption(option, language, value), _language);
        }

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
        {
            return _backingOptionSet.GetChangedOptions(optionSet);
        }
    }
}
