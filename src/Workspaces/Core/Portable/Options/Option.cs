// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: Option<T>

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Options;

/// <inheritdoc cref="Option2{T}"/>
public class Option<T> : IPublicOption
{
    private readonly OptionDefinition _optionDefinition;

    public string Feature { get; }

    internal OptionGroup Group => _optionDefinition.Group;

    public string Name { get; }

    public T DefaultValue => (T)_optionDefinition.DefaultValue!;

    public Type Type => _optionDefinition.Type;

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
               PublicContract.RequireNonNullItems(storageLocations, nameof(storageLocations)).ToImmutableArray(),
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

    internal Option(OptionDefinition optionDefinition, string feature, string name, ImmutableArray<OptionStorageLocation> storageLocations)
    {
        Feature = feature;
        Name = name;
        _optionDefinition = optionDefinition;
        StorageLocations = storageLocations;
    }

    object? IOption.DefaultValue => this.DefaultValue;

    bool IOption.IsPerLanguage => false;

    IPublicOption? IOption2.PublicOption => null;

    OptionDefinition IOption2.Definition => _optionDefinition;

    bool IEquatable<IOption2?>.Equals(IOption2? other) => Equals(other);

    public override string ToString() => this.PublicOptionDefinitionToString();

    public override int GetHashCode() => _optionDefinition.GetHashCode();

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
