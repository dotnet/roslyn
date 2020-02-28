// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public sealed class OptionsCollection : IEnumerable<KeyValuePair<OptionKey, object>>
    {
        private readonly Dictionary<OptionKey, object> _options = new Dictionary<OptionKey, object>();
        private readonly string _languageName;

        public OptionsCollection(string languageName)
        {
            _languageName = languageName;
        }

        public void Add<T>(Option<T> option, T value)
            => _options.Add(new OptionKey(option), value);

        public void Add<T>(Option<CodeStyleOption<T>> option, T value, NotificationOption notification)
            => _options.Add(new OptionKey(option), new CodeStyleOption<T>(value, notification));

        public void Add<T>(PerLanguageOption<T> option, T value)
            => _options.Add(new OptionKey(option, _languageName), value);

        public void Add<T>(PerLanguageOption<CodeStyleOption<T>> option, T value)
            => Add(option, value, option.DefaultValue.Notification);

        public void Add<T>(PerLanguageOption<CodeStyleOption<T>> option, T value, NotificationOption notification)
            => _options.Add(new OptionKey(option, _languageName), new CodeStyleOption<T>(value, notification));

        public IEnumerator<KeyValuePair<OptionKey, object>> GetEnumerator()
            => _options.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
