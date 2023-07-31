// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

internal sealed class Setting<TOptionValue>(
    OptionKey2 optionKey,
    string description,
    OptionUpdater updater,
    SettingLocation location,
    TOptionValue initialValue) : Setting(optionKey, description, updater, location)
{
    /// <summary>
    /// Stores the latest value of the option.
    /// </summary>
    private TOptionValue _value = initialValue;

    public override Type Type
        => typeof(TOptionValue);

    protected override object UpdateValue(object settingValue)
        => _value = (TOptionValue)settingValue;

    public override object? GetValue()
        => _value;
}
