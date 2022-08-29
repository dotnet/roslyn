// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.EditorConfigSettings;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract class WhitespaceSetting<T> : WhitespaceSetting
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

        public override OptionKey2 Key => new(_option);

        private readonly Option2<T> _option;
        private readonly AnalyzerConfigOptions _options;
        private readonly OptionSet _visualStudioOptions;

        public WhitespaceSetting(Option2<T> option,
                                 string description,
                                 AnalyzerConfigOptions options,
                                 OptionSet visualStudioOptions,
                                 OptionUpdater updater,
                                 SettingLocation location,
                                 IEditorConfigData editorConfigData)
            : base(description, updater, location, editorConfigData)
        {
            _option = option;
            _options = options;
            _visualStudioOptions = visualStudioOptions;
        }

        public override void SetValue(object value)
        {
            Value = (T)value;
            Location = Location with { LocationKind = LocationKind.EditorConfig };
            Updater.QueueUpdate(_option, value);
        }

        public override object? GetValue() => Value;
    }
}
