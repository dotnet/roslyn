﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: PerLanguageOption<T>

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    /// <inheritdoc cref="PerLanguageOption2{T}"/>
    public class PerLanguageOption<T> : IPerLanguageValuedOption<T>
    {
        private readonly OptionDefinition _optionDefinition;

        /// <inheritdoc cref="OptionDefinition.Feature"/>
        public string Feature => _optionDefinition.Feature;

        /// <inheritdoc cref="OptionDefinition.Group"/>
        internal OptionGroup Group => _optionDefinition.Group;

        /// <inheritdoc cref="OptionDefinition.Name"/>
        public string Name => _optionDefinition.Name;

        /// <inheritdoc cref="OptionDefinition.Type"/>
        public Type Type => _optionDefinition.Type;

        /// <inheritdoc cref="OptionDefinition.DefaultValue"/>
        public T DefaultValue => (T)_optionDefinition.DefaultValue!;

        /// <inheritdoc cref="PerLanguageOption2{T}.StorageLocations"/>
        public ImmutableArray<OptionStorageLocation> StorageLocations { get; }

        public PerLanguageOption(string feature, string name, T defaultValue)
            : this(feature ?? throw new ArgumentNullException(nameof(feature)),
                   OptionGroup.Default,
                   name ?? throw new ArgumentNullException(nameof(name)),
                   defaultValue,
                   storageLocations: ImmutableArray<OptionStorageLocation>.Empty)
        {
        }

        public PerLanguageOption(string feature, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
            : this(feature ?? throw new ArgumentNullException(nameof(feature)),
                   OptionGroup.Default,
                   name ?? throw new ArgumentNullException(nameof(name)),
                   defaultValue,
                   (storageLocations ?? throw new ArgumentNullException(nameof(storageLocations))).ToImmutableArray())
        {
        }

        internal PerLanguageOption(string? feature, OptionGroup group, string? name, T defaultValue, ImmutableArray<OptionStorageLocation> storageLocations)
            : this(new OptionDefinition(feature, group, name, storageLocations.GetOptionConfigName(feature, name), defaultValue, typeof(T)), storageLocations)
        {
        }

        internal PerLanguageOption(OptionDefinition optionDefinition, ImmutableArray<OptionStorageLocation> storageLocations)
        {
            _optionDefinition = optionDefinition;
            StorageLocations = storageLocations;
        }

        OptionDefinition IOption2.OptionDefinition => _optionDefinition;

        OptionGroup IOptionWithGroup.Group => this.Group;

        object? IOption.DefaultValue => this.DefaultValue;

        bool IOption.IsPerLanguage => true;

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
    }
}
