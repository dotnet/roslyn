// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

internal sealed class CheckBoxEnumFlagsOptionViewModel<TOptionValue> : AbstractCheckBoxViewModel
    where TOptionValue : struct, Enum
{
    private readonly int _flag;
    private readonly Conversions<TOptionValue, int> _conversions;

    /// <summary>
    /// Stores the latest value of the flags.
    /// Shared accross all instances of <see cref="CheckBoxEnumFlagsOptionViewModel{TFlags}"/> that represent bits of the same flags enum.
    /// </summary>
    private readonly StrongBox<TOptionValue> _valueStorage;

    public CheckBoxEnumFlagsOptionViewModel(
        IOption2 option,
        int flag,
        string description,
        string preview,
        AbstractOptionPreviewViewModel info,
        OptionStore optionStore,
        StrongBox<TOptionValue> valueStorage,
        Conversions<TOptionValue, int> conversions)
        : this(option, flag, description, preview, preview, info, optionStore, valueStorage, conversions)
    {
    }

    public CheckBoxEnumFlagsOptionViewModel(
        IOption2 option,
        int flag,
        string description,
        string falsePreview,
        string truePreview,
        AbstractOptionPreviewViewModel info,
        OptionStore optionStore,
        StrongBox<TOptionValue> valueStorage,
        Conversions<TOptionValue, int> conversions)
        : base(option, description, truePreview, falsePreview, info)
    {
        _valueStorage = valueStorage;
        _flag = flag;
        _conversions = conversions;

        var flags = optionStore.GetOption<TOptionValue>(option, option.IsPerLanguage ? info.Language : null);
        _valueStorage.Value = flags;

        SetProperty(ref _isChecked, (conversions.To(flags) & flag) == flag);
    }

    public override bool IsChecked
    {
        get
        {
            return _isChecked;
        }

        set
        {
            SetProperty(ref _isChecked, value);

            var flags = _conversions.To(_valueStorage.Value);
            if (value)
            {
                flags |= _flag;
            }
            else
            {
                flags &= ~_flag;
            }

            _valueStorage.Value = _conversions.From(flags);

            Info.SetOptionAndUpdatePreview(_valueStorage.Value, Option, GetPreview());
        }
    }
}
