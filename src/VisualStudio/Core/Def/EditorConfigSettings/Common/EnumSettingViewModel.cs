// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

internal abstract class EnumSettingViewModel<T> : IEnumSettingViewModel
    where T : Enum
{
    private readonly IReadOnlyDictionary<string, T> _mapping;

    public string[] EnumValues => GetValueDescriptions();

    public string ToolTip => ServicesVSResources.Value;
    public string AutomationName => ServicesVSResources.Value;

    private string? _selectedEnumValue;

    public string SelectedEnumValue
    {
        get
        {
            if (_selectedEnumValue is null)
            {
                var values = EnumValues;
                var index = GetValueIndex();
                _selectedEnumValue = values[index];
            }

            return _selectedEnumValue;
        }
        set => _selectedEnumValue = value;
    }

    protected EnumSettingViewModel()
    {
        _mapping = GetValuesAndDescriptions();
    }

    public void ChangeProperty(string propertyDescription)
    {
        if (_mapping.TryGetValue(propertyDescription, out var value))
        {
            ChangePropertyTo(value);
        }
    }

    public string[] GetValueDescriptions()
        => _mapping.Keys.ToArray();

    public int GetValueIndex()
    {
        var value = GetCurrentValue();
        return _mapping.Values.ToList().IndexOf(value);
    }

    protected abstract IReadOnlyDictionary<string, T> GetValuesAndDescriptions();
    protected abstract void ChangePropertyTo(T newValue);
    protected abstract T GetCurrentValue();
}
