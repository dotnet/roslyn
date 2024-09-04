// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

namespace Microsoft.CodeAnalysis.Options;

internal abstract class OptionDefinition : IEquatable<OptionDefinition?>
{
    // editorconfig name prefixes used for C#/VB specific options:
    public const string CSharpConfigNamePrefix = "csharp_";
    public const string VisualBasicConfigNamePrefix = "visual_basic_";

    // editorconfig name prefix use for options that apply to both languages:
    public const string LanguageAgnosticConfigNamePrefix = "dotnet_";

    // editorconfig name prefix for feature options that are read from editorconfig 
    // file but are currently not intended for users to be set in the editorconfig file.
    public const string InternalConfigNamePrefix = "internal_";

    /// <summary>
    /// Optional group/sub-feature for this option.
    /// </summary>
    internal OptionGroup Group { get; }

    /// <summary>
    /// A unique name of the option used in editorconfig.
    /// </summary>
    public string ConfigName { get; }

    /// <summary>
    /// True if the value of the option may be stored in an editorconfig file.
    /// </summary>
    public bool IsEditorConfigOption { get; }

    /// <summary>
    ///  Mapping between the public option storage and internal option storage.
    /// </summary>
    public OptionStorageMapping? StorageMapping { get; }

    /// <summary>
    /// The untyped/boxed default value of the option.
    /// </summary>
    public object? DefaultValue { get; }

    public OptionDefinition(OptionGroup? group, string configName, object? defaultValue, OptionStorageMapping? storageMapping, bool isEditorConfigOption)
    {
        ConfigName = configName;
        Group = group ?? OptionGroup.Default;
        StorageMapping = storageMapping;
        IsEditorConfigOption = isEditorConfigOption;
        DefaultValue = defaultValue;

        Debug.Assert(IsSupportedOptionType(Type));
    }

    /// <summary>
    /// The type of the option value.
    /// </summary>
    public abstract Type Type { get; }

    public IEditorConfigValueSerializer Serializer => SerializerImpl;

    protected abstract IEditorConfigValueSerializer SerializerImpl { get; }

    public override bool Equals(object? other)
        => Equals(other as OptionDefinition);

    public bool Equals(OptionDefinition? other)
        => ConfigName == other?.ConfigName;

    public override int GetHashCode()
        => ConfigName.GetHashCode();

    public override string ToString()
        => ConfigName;

    public static bool operator ==(OptionDefinition? left, OptionDefinition? right)
        => ReferenceEquals(left, right) || left?.Equals(right) == true;

    public static bool operator !=(OptionDefinition? left, OptionDefinition? right)
        => !(left == right);

    public static bool IsSupportedOptionType(Type type)
        => type == typeof(bool) ||
           type == typeof(string) ||
           type == typeof(int) ||
           type == typeof(long) ||
           type == typeof(bool?) ||
           type == typeof(int?) ||
           type == typeof(long?) ||
           type.IsEnum ||
           Nullable.GetUnderlyingType(type)?.IsEnum == true ||
#if !CODE_STYLE
           typeof(ICodeStyleOption).IsAssignableFrom(type) ||
#endif
           typeof(ICodeStyleOption2).IsAssignableFrom(type) ||
           type == typeof(NamingStylePreferences) ||
           type == typeof(ImmutableArray<bool>) ||
           type == typeof(ImmutableArray<string>) ||
           type == typeof(ImmutableArray<int>) ||
           type == typeof(ImmutableArray<long>);
}

internal sealed class OptionDefinition<T>(
    T defaultValue,
    EditorConfigValueSerializer<T>? serializer,
    OptionGroup? group,
    string configName,
    OptionStorageMapping? storageMapping,
    bool isEditorConfigOption) : OptionDefinition(group, configName, defaultValue, storageMapping, isEditorConfigOption)
{
    public new T DefaultValue { get; } = defaultValue;
    public new EditorConfigValueSerializer<T> Serializer { get; } = serializer ?? EditorConfigValueSerializer.Default<T>();

    public override Type Type
        => typeof(T);

    protected override IEditorConfigValueSerializer SerializerImpl
        => Serializer;
}
