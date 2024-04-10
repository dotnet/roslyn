// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

public abstract partial class OptionSet : IOptionsReader
{
    internal static readonly OptionSet Empty = new EmptyOptionSet();

    protected OptionSet()
    {
    }

    internal abstract object? GetInternalOptionValue(OptionKey optionKey);

    /// <summary>
    /// Gets the value of the option, or the default value if not otherwise set.
    /// </summary>
    public object? GetOption(OptionKey optionKey)
    {
        if (optionKey.Option is IOption2 { Definition.StorageMapping: { } mapping })
        {
            return mapping.ToPublicOptionValue(GetInternalOptionValue(new OptionKey(mapping.InternalOption, optionKey.Language)));
        }

        var result = GetInternalOptionValue(optionKey);
        Debug.Assert(IsPublicOptionValue(result));
        return result;
    }

    /// <summary>
    /// Gets the value of the option, or the default value if not otherwise set.
    /// </summary>
    public T GetOption<T>(OptionKey optionKey)
        => (T)GetOption(optionKey)!;

#pragma warning disable RS0030 // Do not used banned APIs: PerLanguageOption<T>
    /// <summary>
    /// Gets the value of the option, or the default value if not otherwise set.
    /// </summary>
    public T GetOption<T>(Option<T> option)
        => GetOption<T>(new OptionKey(option));

    /// <summary>
    /// Creates a new <see cref="OptionSet" /> that contains the changed value.
    /// </summary>
    public OptionSet WithChangedOption<T>(Option<T> option, T value)
        => WithChangedOption(new OptionKey(option), value);

    /// <summary>
    /// Gets the value of the option, or the default value if not otherwise set.
    /// </summary>
    public T GetOption<T>(PerLanguageOption<T> option, string? language)
        => GetOption<T>(new OptionKey(option, language));

    /// <summary>
    /// Creates a new <see cref="OptionSet" /> that contains the changed value.
    /// </summary>
    public OptionSet WithChangedOption<T>(PerLanguageOption<T> option, string? language, T value)
        => WithChangedOption(new OptionKey(option, language), value);
#pragma warning restore

    /// <summary>
    /// Creates a new <see cref="OptionSet" /> that contains the changed value.
    /// </summary>
    public virtual OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value)
    {
        if (optionAndLanguage.Option is IOption2 { Definition.StorageMapping: { } mapping })
        {
            var mappedOptionKey = new OptionKey(mapping.InternalOption, optionAndLanguage.Language);
            var currentValue = GetInternalOptionValue(mappedOptionKey);
            return WithChangedOptionInternal(mappedOptionKey, mapping.UpdateInternalOptionValue(currentValue, value));
        }

        return WithChangedOptionInternal(optionAndLanguage, value);
    }

    internal virtual OptionSet WithChangedOptionInternal(OptionKey optionKey, object? internalValue)
        => throw ExceptionUtilities.Unreachable();

    bool IOptionsReader.TryGetOption<T>(OptionKey2 optionKey, out T value)
    {
        value = (T)GetInternalOptionValue(new OptionKey(optionKey.Option, optionKey.Language))!;
        return true;
    }

    /// <summary>
    /// Checks if the value is an internal representation -- does not cover all cases, just code style options.
    /// </summary>
    internal static bool IsInternalOptionValue(object? value)
        => value is not ICodeStyleOption codeStyle || ReferenceEquals(codeStyle, codeStyle.AsInternalCodeStyleOption());

    /// <summary>
    /// Checks if the value is an public representation -- does not cover all cases, just code style options.
    /// </summary>
    internal static bool IsPublicOptionValue(object? value)
        => value is not ICodeStyleOption codeStyle || ReferenceEquals(codeStyle, codeStyle.AsPublicCodeStyleOption());
}
