// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal sealed class FormattingSetting<T> : FormattingSetting
        where T : notnull
    {
        public override bool IsDefinedInEditorConfig => _options.TryGetEditorConfigOption<T>(_option, out _);

        private bool _isValueSet;
        private T? _value;
        public T Value
        {
            private set
            {
                if (!_isValueSet)
                {
                    _isValueSet = true;
                }

                _value = value;
            }
            get
            {
                if (_value is not null && _isValueSet)
                {
                    return _value;
                }

                if (_options.TryGetEditorConfigOption(_option, out T? value) &&
                    value is not null)
                {
                    return value;
                }

                return _visualStudioOptions.GetOption(_option);
            }
        }

        public override Type Type => typeof(T);
        public override string Category => _option.Group.Description;

        public override OptionKey2 Key => new(_option, _option.OptionDefinition.IsPerLanguage ? Language ?? LanguageNames.CSharp : null);

        private readonly Option2<T> _option;
        private readonly AnalyzerConfigOptions _options;
        private readonly OptionSet _visualStudioOptions;

        public FormattingSetting(Option2<T> option,
                                 string description,
                                 AnalyzerConfigOptions options,
                                 OptionSet visualStudioOptions,
                                 OptionUpdater updater)
            : base(description, updater)
        {
            _option = option;
            _options = options;
            _visualStudioOptions = visualStudioOptions;
        }

        public override void SetValue(object value)
        {
            Value = (T)value;
            Updater.QueueUpdate(_option, value);
        }

        public override object? GetValue() => Value;
    }
}
