// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

#if !CODE_STYLE
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Linq;
using Xunit;
#endif

#if !NETCOREAPP
using System;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    internal sealed class OptionsCollection(string languageName) : IReadOnlyCollection<KeyValuePair<OptionKey2, object?>>, IOptionsReader
    {
        private readonly Dictionary<OptionKey2, object?> _options = [];

        public string LanguageName => languageName;
        public string DefaultExtension => languageName == LanguageNames.CSharp ? "cs" : "vb";

        public int Count => _options.Count;

        public void Add<T>(OptionKey2 optionKey, T value)
        {
            // Can only add internally defined option whose storage is not mapped to another option:
            Debug.Assert(optionKey.Option is IOption2 { Definition.StorageMapping: null });
            _options.Add(optionKey, value);
        }

        public void Set<T>(Option2<T> option, T value)
            => _options[new OptionKey2(option)] = value;

        public void Add<T>(Option2<T> option, T value)
            => Add(new OptionKey2(option), value);

        public void Add<T>(Option2<CodeStyleOption2<T>> option, T value)
            => Add(option, value, option.DefaultValue.Notification);

        public void Add<T>(Option2<CodeStyleOption2<T>> option, T value, NotificationOption2 notification)
            => Add(new OptionKey2(option), new CodeStyleOption2<T>(value, notification));

        public void Add<T>(PerLanguageOption2<T> option, T value)
            => Add(new OptionKey2(option, languageName), value);

        public void Add<T>(PerLanguageOption2<CodeStyleOption2<T>> option, T value)
            => Add(option, value, option.DefaultValue.Notification);

        public void Add<T>(PerLanguageOption2<CodeStyleOption2<T>> option, T value, NotificationOption2 notification)
            => Add(new OptionKey2(option, languageName), new CodeStyleOption2<T>(value, notification));

        // 📝 This can be removed if/when collection initializers support AddRange.
        public void Add(OptionsCollection? options)
            => AddRange(options);

        public void AddRange(OptionsCollection? options)
        {
            if (options is null)
                return;

            foreach (var (key, value) in options)
                _options.Add(key, value);
        }

        public IEnumerator<KeyValuePair<OptionKey2, object?>> GetEnumerator()
            => _options.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

#if !CODE_STYLE
        public OptionSet ToOptionSet()
            => new TestOptionSet(_options.ToImmutableDictionary(entry => new OptionKey(entry.Key.Option, entry.Key.Language), entry => entry.Value));

        public void SetGlobalOptions(IGlobalOptionService globalOptions)
        {
            foreach (var (optionKey, value) in _options)
            {
                globalOptions.SetGlobalOption(optionKey, value);
            }
        }

        public StructuredAnalyzerConfigOptions ToAnalyzerConfigOptions()
        {
            Assert.All(this, o => Assert.True(o.Key.Option.Definition.IsEditorConfigOption));

            return StructuredAnalyzerConfigOptions.Create(
               new DictionaryAnalyzerConfigOptions(
                    this.Select(static o => new KeyValuePair<string, string>(o.Key.Option.Definition.ConfigName, o.Key.Option.Definition.Serializer.Serialize(o.Value))).ToImmutableDictionary()));
        }
#endif

        public bool TryGetOption<T>(OptionKey2 optionKey, out T value)
        {
            if (_options.TryGetValue(optionKey, out var objValue))
            {
                value = (T)objValue!;
                return true;
            }

            value = default!;
            return false;
        }
    }
}
