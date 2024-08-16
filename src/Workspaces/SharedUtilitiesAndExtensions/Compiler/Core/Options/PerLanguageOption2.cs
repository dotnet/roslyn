// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Marker interface for <see cref="PerLanguageOption2{T}"/>.
/// This option may apply to multiple languages, such that the option can have a different value for each language.
/// </summary>
internal interface IPerLanguageValuedOption : IOption2
{
}

/// <inheritdoc cref="IPerLanguageValuedOption"/>
internal interface IPerLanguageValuedOption<T> : IPerLanguageValuedOption
{
}

/// <summary>
/// An option that can be specified once per language.
/// </summary>
/// <typeparam name="T"></typeparam>
internal partial class PerLanguageOption2<T> : IPerLanguageValuedOption<T>
{
    public OptionDefinition<T> Definition { get; }
    public IPublicOption? PublicOption { get; }

    internal PerLanguageOption2(OptionDefinition<T> optionDefinition, Func<IOption2, IPublicOption>? publicOptionFactory)
    {
        Definition = optionDefinition;
        PublicOption = publicOptionFactory?.Invoke(this);
    }

    public PerLanguageOption2(
        string name,
        T defaultValue,
        OptionGroup? group = null,
        bool isEditorConfigOption = false,
        EditorConfigValueSerializer<T>? serializer = null)
        : this(new OptionDefinition<T>(defaultValue, serializer, group, name, storageMapping: null, isEditorConfigOption), publicOptionFactory: null)
    {
        VerifyNamingConvention();
    }

    [Conditional("DEBUG")]
    private void VerifyNamingConvention()
    {
        // TODO: remove, once all options have editorconfig-like name https://github.com/dotnet/roslyn/issues/65787
        if (!Definition.IsEditorConfigOption)
        {
            return;
        }

        // options with per-language values shouldn't have language-specific prefix
        Debug.Assert(!Definition.ConfigName.StartsWith(OptionDefinition.CSharpConfigNamePrefix, StringComparison.Ordinal));
        Debug.Assert(!Definition.ConfigName.StartsWith(OptionDefinition.VisualBasicConfigNamePrefix, StringComparison.Ordinal));
    }

    OptionDefinition IOption2.Definition => Definition;
    public T DefaultValue => Definition.DefaultValue;

#if CODE_STYLE
    bool IOption2.IsPerLanguage => true;
#else
    string IOption.Feature => "config";
    string IOption.Name => Definition.ConfigName;
    object? IOption.DefaultValue => Definition.DefaultValue;
    bool IOption.IsPerLanguage => true;
    Type IOption.Type => Definition.Type;
    ImmutableArray<OptionStorageLocation> IOption.StorageLocations => [];
#endif
    public override string ToString() => Definition.ToString();

    public override int GetHashCode() => Definition.GetHashCode();

    public override bool Equals(object? obj) => Equals(obj as IOption2);

    public bool Equals(IOption2? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Definition == other?.Definition;
    }
}
