// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

internal sealed class CodeStyleSetting<T> : CodeStyleSetting
    where T : notnull
{
    private readonly T[] _possibleValues;
    private readonly string[] _valueDescriptions;

    /// <summary>
    /// Stores the latest value of the option.
    /// </summary>
    private CodeStyleOption2<T> _value;

    public CodeStyleSetting(
        OptionKey2 optionKey,
        string description,
        OptionUpdater updater,
        SettingLocation location,
        CodeStyleOption2<T> initialValue,
        T[] possibleValues,
        string[] valueDescriptions)
        : base(optionKey, description, updater, location)
    {
        Contract.ThrowIfFalse(possibleValues.Length == valueDescriptions.Length);

        _value = initialValue;
        _possibleValues = possibleValues;
        _valueDescriptions = valueDescriptions;
    }

    public override Type Type
        => typeof(T);

    public override string[] GetValueDescriptions()
        => _valueDescriptions;

    public override string GetCurrentValueDescription()
        => _valueDescriptions[_possibleValues.IndexOf(_value.Value)];

    public override ICodeStyleOption GetCodeStyle()
        => _value;

    protected override object GetPossibleValue(int valueIndex)
        => _possibleValues[valueIndex];

    protected override object UpdateValue(object settingValue)
        => _value = (CodeStyleOption2<T>)settingValue;
}
