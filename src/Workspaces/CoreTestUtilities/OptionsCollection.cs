// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

#if !NETCOREAPP
using System;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    internal sealed class OptionsCollection : IReadOnlyCollection<KeyValuePair<OptionKey2, object?>>
    {
        private readonly Dictionary<OptionKey2, object?> _options = new Dictionary<OptionKey2, object?>();
        private readonly string _languageName;

        public OptionsCollection(string languageName)
        {
            _languageName = languageName;
        }

        public string DefaultExtension => _languageName == LanguageNames.CSharp ? "cs" : "vb";

        public int Count => _options.Count;

        public void Set<T>(Option2<T> option, T value)
            => _options[new OptionKey2(option)] = value;

        public void Add<T>(Option2<T> option, T value)
            => _options.Add(new OptionKey2(option), value);

        public void Add<T>(Option2<CodeStyleOption2<T>> option, T value)
            => Add(option, value, option.DefaultValue.Notification);

        public void Add<T>(Option2<CodeStyleOption2<T>> option, T value, NotificationOption2 notification)
            => _options.Add(new OptionKey2(option), new CodeStyleOption2<T>(value, notification));

        public void Add<T>(PerLanguageOption2<T> option, T value)
            => _options.Add(new OptionKey2(option, _languageName), value);

        public void Add<T>(PerLanguageOption2<CodeStyleOption2<T>> option, T value)
            => Add(option, value, option.DefaultValue.Notification);

        public void Add<T>(PerLanguageOption2<CodeStyleOption2<T>> option, T value, NotificationOption2 notification)
            => _options.Add(new OptionKey2(option, _languageName), new CodeStyleOption2<T>(value, notification));

        public void AddRange(OptionsCollection options)
        {
            foreach (var (key, value) in options)
            {
                _options.Add(key, value);
            }
        }

        public IEnumerator<KeyValuePair<OptionKey2, object?>> GetEnumerator()
            => _options.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
