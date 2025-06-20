// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: Option<T>

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Options;

/// <inheritdoc cref="Option2{T}"/>
public class Option<T> : IPublicOption
{
    internal readonly OptionDefinition<T> OptionDefinition;

    public string Feature { get; }

    internal OptionGroup Group => OptionDefinition.Group;

    public string Name { get; }

    public T DefaultValue => (T)OptionDefinition.DefaultValue!;

    public Type Type => OptionDefinition.Type;

    public ImmutableArray<OptionStorageLocation> StorageLocations { get; }

    [Obsolete("Use a constructor that specifies an explicit default value.")]
    public Option(string feature, string name)
        : this(feature, name, default!)
    {
        // This constructor forwards to the next one; it exists to maintain source-level compatibility with older callers.
    }

    public Option(string feature, string name, T defaultValue)
        : this(feature ?? throw new ArgumentNullException(nameof(feature)),
               name ?? throw new ArgumentNullException(nameof(name)),
               OptionGroup.Default,
               defaultValue,
               storageLocations: [],
               storageMapping: null,
               isEditorConfigOption: false)
    {
    }

    public Option(string feature, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
        : this(feature ?? throw new ArgumentNullException(nameof(feature)),
               name ?? throw new ArgumentNullException(nameof(name)),
               OptionGroup.Default,
               defaultValue,
               [.. PublicContract.RequireNonNullItems(storageLocations, nameof(storageLocations))],
               storageMapping: null,
               isEditorConfigOption: false)
    {
        // should not be used internally to create options
        Debug.Assert(storageLocations.All(l => l is not IEditorConfigValueSerializer));
    }

    internal Option(
        string feature,
        string name,
        OptionGroup group,
        T defaultValue,
        ImmutableArray<OptionStorageLocation> storageLocations,
        OptionStorageMapping? storageMapping,
        bool isEditorConfigOption)
        : this(new OptionDefinition<T>(defaultValue, EditorConfigValueSerializer<T>.Unsupported, group, feature + "_" + name, storageMapping, isEditorConfigOption), feature, name, storageLocations)
    {
    }

    internal Option(OptionDefinition<T> optionDefinition, string feature, string name, ImmutableArray<OptionStorageLocation> storageLocations)
    {
        Feature = feature;
        Name = name;
        OptionDefinition = optionDefinition;
        StorageLocations = storageLocations;
    }

    object? IOption.DefaultValue => this.DefaultValue;

    bool IOption.IsPerLanguage => false;

    IPublicOption? IOption2.PublicOption => null;

    OptionDefinition IOption2.Definition => OptionDefinition;

    bool IEquatable<IOption2?>.Equals(IOption2? other) => Equals(other);

    public override string ToString() => this.PublicOptionDefinitionToString();

    public override int GetHashCode() => OptionDefinition.GetHashCode();

    public override bool Equals(object? obj) => Equals(obj as IOption2);

    private bool Equals(IOption2? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null && this.PublicOptionDefinitionEquals(other);
    }

    public static implicit operator OptionKey(Option<T> option)
        => new(option);
}

internal static class OptionUtilities
{
    /// <summary>
    /// Allows an option of one enum type to be converted to another enum type, provided that both enums share the same underlying type.
    /// Useful for some cases in Roslyn where we have an existing shipped public option in the Workspace layer, and an internal option
    /// in the CodeStyle layer, and we want to map between them.
    /// </summary>
    public static Option<TToEnum> ConvertEnumOption<TFromEnum, TToEnum, TUnderlyingEnumType>(Option<TFromEnum> option)
        where TFromEnum : struct, Enum
        where TToEnum : struct, Enum
        where TUnderlyingEnumType : struct
    {
        // Ensure that this is only called for enums that are actually compatible with each other.
        Contract.ThrowIfTrue(typeof(TFromEnum).GetEnumUnderlyingType() != typeof(TUnderlyingEnumType));
        Contract.ThrowIfTrue(typeof(TToEnum).GetEnumUnderlyingType() != typeof(TUnderlyingEnumType));

        var definition = option.OptionDefinition;
        var newDefaultValue = ConvertEnum<TFromEnum, TToEnum, TUnderlyingEnumType>(definition.DefaultValue);
        var newSerializer = ConvertEnumSerializer<TFromEnum, TToEnum, TUnderlyingEnumType>(definition.Serializer);

        var newDefinition = new OptionDefinition<TToEnum>(
            defaultValue: newDefaultValue, newSerializer, definition.Group, definition.ConfigName, definition.StorageMapping, definition.IsEditorConfigOption);

        return new(newDefinition, option.Feature, option.Name, option.StorageLocations);
    }

    private static EditorConfigValueSerializer<TToEnum>? ConvertEnumSerializer<TFromEnum, TToEnum, TUnderlyingEnumType>(EditorConfigValueSerializer<TFromEnum> serializer)
        where TFromEnum : struct
        where TToEnum : struct
        where TUnderlyingEnumType : struct
    {
        return new(
            value => ConvertEnum<TFromEnum, TToEnum, TUnderlyingEnumType>(serializer.ParseValue(value)),
            value => serializer.SerializeValue(ConvertEnum<TToEnum, TFromEnum, TUnderlyingEnumType>(value)));
    }

    private static Optional<TToEnum> ConvertEnum<TFromEnum, TToEnum, TUnderlyingEnumType>(Optional<TFromEnum> optional)
        where TFromEnum : struct
        where TToEnum : struct
        where TUnderlyingEnumType : struct
    {
        if (!optional.HasValue)
            return default;

        return ConvertEnum<TFromEnum, TToEnum, TUnderlyingEnumType>(optional.Value);
    }

    private static TToEnum ConvertEnum<TFromEnum, TToEnum, TUnderlyingEnumType>(TFromEnum value)
        where TFromEnum : struct
        where TToEnum : struct
        where TUnderlyingEnumType : struct
    {
        return Unsafe.As<TUnderlyingEnumType, TToEnum>(ref Unsafe.As<TFromEnum, TUnderlyingEnumType>(ref value));
    }
}
