// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
#if CODE_STYLE
    internal interface IOptionsCollection : IReadOnlyCollection<KeyValuePair<OptionKey2, object>>
    {
        string GetEditorConfigText();
    }
#endif

    internal sealed class OptionsCollection : IReadOnlyCollection<KeyValuePair<OptionKey2, object>>
#if CODE_STYLE
        , IOptionsCollection
#endif
    {
        private readonly Dictionary<OptionKey2, object> _options = new Dictionary<OptionKey2, object>();
        private readonly string _languageName;
        private readonly string _defaultExtension;

        public OptionsCollection(string languageName, params (OptionKey2 key, object value)[] options)
        {
            _languageName = languageName;
            _defaultExtension = languageName == LanguageNames.CSharp ? "cs" : "vb";

            foreach (var (key, value) in options)
            {
                Add(key, value);
            }

        }

        public int Count => _options.Count;

        public void Add<T>(Option2<T> option, T value)
            => _options.Add(new OptionKey2(option), value);

        public void Add<T>(Option2<CodeStyleOption2<T>> option, T value, NotificationOption2 notification)
            => _options.Add(new OptionKey2(option), new CodeStyleOption2<T>(value, notification));

        public void Add<T>(PerLanguageOption2<T> option, T value)
            => _options.Add(new OptionKey2(option, _languageName), value);

        public void Add<T>(PerLanguageOption2<CodeStyleOption2<T>> option, T value)
            => Add(option, value, option.DefaultValue.Notification);

        public void Add<T>(PerLanguageOption2<CodeStyleOption2<T>> option, T value, NotificationOption2 notification)
            => _options.Add(new OptionKey2(option, _languageName), new CodeStyleOption2<T>(value, notification));

        public void Add<T>(OptionKey2 optionKey, T value)
            => _options.Add(optionKey, value);

        public IEnumerator<KeyValuePair<OptionKey2, object>> GetEnumerator()
            => _options.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public string GetEditorConfigText()
        {
            var (text, _) = CodeFixVerifierHelper.ConvertOptionsToAnalyzerConfig(_defaultExtension, explicitEditorConfig: string.Empty, this);
            return text.ToString();
        }
    }
}
