// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: Option<T>

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Options
{
    /// <inheritdoc cref="Option2{T}"/>
    public class Option<T> : ISingleValuedOption<T>
    {
        private readonly OptionDefinition _optionDefinition;

        /// <inheritdoc cref="OptionDefinition.Feature"/>
        public string Feature => _optionDefinition.Feature;

        /// <inheritdoc cref="OptionDefinition.Group"/>
        internal OptionGroup Group => _optionDefinition.Group;

        /// <inheritdoc cref="OptionDefinition.Name"/>
        public string Name => _optionDefinition.Name;

        /// <inheritdoc cref="OptionDefinition.DefaultValue"/>
        public T DefaultValue => (T)_optionDefinition.DefaultValue!;

        /// <inheritdoc cref="OptionDefinition.Type"/>
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
                   OptionGroup.Default,
                   name ?? throw new ArgumentNullException(nameof(name)),
                   defaultValue,
                   storageLocations: ImmutableArray<OptionStorageLocation>.Empty,
                   isEditorConfigOption: false)
        {
        }

        public Option(string feature, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
            : this(feature ?? throw new ArgumentNullException(nameof(feature)),
                   OptionGroup.Default,
                   name ?? throw new ArgumentNullException(nameof(name)),
                   defaultValue,
                   PublicContract.RequireNonNullItems(storageLocations, nameof(storageLocations)).ToImmutableArray(),
                   isEditorConfigOption: false)
        {
            // should not be used internally to create options
            Debug.Assert(storageLocations.All(l => l is not IEditorConfigStorageLocation));
        }

        internal Option(string? feature, OptionGroup group, string? name, T defaultValue, ImmutableArray<OptionStorageLocation> storageLocations, bool isEditorConfigOption)
            : this(new OptionDefinition(feature, group, name, storageLocations.GetOptionConfigName(feature, name), defaultValue, typeof(T), isEditorConfigOption), storageLocations)
        {
        }

        internal Option(OptionDefinition optionDefinition, ImmutableArray<OptionStorageLocation> storageLocations)
        {
            _optionDefinition = optionDefinition;
            StorageLocations = storageLocations;
        }

        IEditorConfigStorageLocation? IOption2.StorageLocation
            => StorageLocations is [IEditorConfigStorageLocation location] ? location : null;

        object? IOption.DefaultValue => this.DefaultValue;

        bool IOption.IsPerLanguage => false;

        OptionDefinition IOption2.OptionDefinition => _optionDefinition;

        string? ISingleValuedOption.LanguageName
        {
            get
            {
                Debug.Fail("It's not expected that we access LanguageName property for Option<T>. The options we use should be Option2<T>.");
                return null;
            }
        }

        bool IEquatable<IOption2?>.Equals(IOption2? other) => Equals(other);

        public override string ToString() => _optionDefinition.PublicOptionDefinitionToString();

        public override int GetHashCode() => _optionDefinition.GetHashCode();

        public override bool Equals(object? obj) => Equals(obj as IOption2);

        private bool Equals(IOption2? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other is not null && _optionDefinition.PublicOptionDefinitionEquals(other.OptionDefinition);
        }

        public static implicit operator OptionKey(Option<T> option)
            => new(option);
    }
}
