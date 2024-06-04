// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Marker interface for options that has the same value for all languages.
/// </summary>
internal interface ISingleValuedOption : IOption2
{
    /// <summary>
    /// The language name that supports this option, or null if it's supported by multiple languages.
    /// </summary>
    /// <remarks>
    /// This is an optional metadata used for:
    /// <list type="bullet">
    /// <item><description>Analyzer id to option mapping, used (for example) by configure code-style code action</description></item>
    /// <item><description>EditorConfig UI to determine whether to put this option under <c>[*.cs]</c>, <c>[*.vb]</c>, or <c>[*.{cs,vb}]</c></description></item>
    /// </list>
    /// Note that this property is not (and should not be) used for computing option values or storing options.
    /// </remarks>
    public string? LanguageName { get; }
}

/// <inheritdoc cref="ISingleValuedOption"/>
internal interface ISingleValuedOption<T> : ISingleValuedOption
{
}

internal partial class Option2<T> : ISingleValuedOption<T>
{
    public OptionDefinition<T> Definition { get; }
    public IPublicOption? PublicOption { get; }
    public string? LanguageName { get; }

    internal Option2(OptionDefinition<T> definition, string? languageName, Func<IOption2, IPublicOption>? publicOptionFactory)
    {
        Definition = definition;
        LanguageName = languageName;
        PublicOption = publicOptionFactory?.Invoke(this);
    }

    public Option2(
        string name,
        T defaultValue,
        OptionGroup? group = null,
        string? languageName = null,
        bool isEditorConfigOption = false,
        EditorConfigValueSerializer<T>? serializer = null)
        : this(new OptionDefinition<T>(defaultValue, serializer, group, name, storageMapping: null, isEditorConfigOption), languageName, publicOptionFactory: null)
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

        Debug.Assert(LanguageName is null == (Definition.ConfigName.StartsWith(OptionDefinition.LanguageAgnosticConfigNamePrefix, StringComparison.Ordinal) ||
            Definition.ConfigName is "file_header_template" or "insert_final_newline"));
        Debug.Assert(LanguageName is LanguageNames.CSharp == Definition.ConfigName.StartsWith(OptionDefinition.CSharpConfigNamePrefix, StringComparison.Ordinal));
        Debug.Assert(LanguageName is LanguageNames.VisualBasic == Definition.ConfigName.StartsWith(OptionDefinition.VisualBasicConfigNamePrefix, StringComparison.Ordinal));
    }

    public T DefaultValue => Definition.DefaultValue;
    OptionDefinition IOption2.Definition => Definition;

#if CODE_STYLE
    bool IOption2.IsPerLanguage => false;
#else
    string IOption.Feature => "config";
    string IOption.Name => Definition.ConfigName;
    object? IOption.DefaultValue => Definition.DefaultValue;
    bool IOption.IsPerLanguage => false;
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

    public static implicit operator OptionKey2(Option2<T> option)
        => new(option);
}
