// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

internal sealed class EnumFlagsSetting<TOptionValue> : Setting
    where TOptionValue : struct, Enum
{
    private readonly int _flag;
    private readonly Conversions<TOptionValue, int> _conversions;

    /// <summary>
    /// Stores the latest value of the flags.
    /// Shared accross all instances of <see cref="EnumFlagsSetting{TFlags}"/> that represent bits of the same flags enum.
    /// </summary>
    private readonly StrongBox<TOptionValue> _valueStorage;

    public EnumFlagsSetting(
        OptionKey2 optionKey,
        string description,
        OptionUpdater updater,
        SettingLocation location,
        int flag,
        StrongBox<TOptionValue> valueStorage,
        Conversions<TOptionValue, int> conversions)
        : base(optionKey, description, updater, location)
    {
        _flag = flag;
        _conversions = conversions;
        _valueStorage = valueStorage;
    }

    public override Type Type
        => typeof(bool);

    protected override object UpdateValue(object settingValue)
    {
        var flags = _conversions.To(_valueStorage.Value);
        if ((bool)settingValue)
        {
            flags |= _flag;
        }
        else
        {
            flags &= ~_flag;
        }

        return _valueStorage.Value = _conversions.From(flags);
    }

    public override object? GetValue()
        => (_conversions.To(_valueStorage.Value) & _flag) == _flag;
}
