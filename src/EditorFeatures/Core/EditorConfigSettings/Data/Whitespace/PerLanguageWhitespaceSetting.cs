// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal sealed class PerLanguageWhitespaceSetting<T> : WhitespaceSetting
        where T : notnull
    {
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

                if (_editorConfigOptions.TryGetEditorConfigOption(_option, out T? value) &&
                    value is not null)
                {
                    return value;
                }

                return (T)_visualStudioOptions.GetOption(Key)!;
            }
        }

        private readonly PerLanguageOption2<T> _option;
        private readonly AnalyzerConfigOptions _editorConfigOptions;
        private readonly OptionSet _visualStudioOptions;

        public PerLanguageWhitespaceSetting(PerLanguageOption2<T> option,
                                            string description,
                                            AnalyzerConfigOptions editorConfigOptions,
                                            OptionSet visualStudioOptions,
                                            OptionUpdater updater,
                                            SettingLocation location)
            : base(description, updater, location)
        {
            _option = option;
            _editorConfigOptions = editorConfigOptions;
            _visualStudioOptions = visualStudioOptions;
        }

        public override string Category => _option.Group.Description;
        public override Type Type => typeof(T);

        public override OptionKey2 Key => new(_option, Language ?? LanguageNames.CSharp);

        public override bool IsDefinedInEditorConfig => _editorConfigOptions.TryGetEditorConfigOption<T>(_option, out _);

        public override void SetValue(object value)
        {
            Value = (T)value;
            Location = Location with { LocationKind = LocationKind.EditorConfig };
            Updater.QueueUpdate(_option, value);
        }

        public override object? GetValue() => Value;
    }
}
