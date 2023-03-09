// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: PerLanguageOption<T>

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Options
{
    /// <inheritdoc cref="PerLanguageOption2{T}"/>
    public class PerLanguageOption<T> : IPublicOption
    {
        private readonly OptionDefinition _optionDefinition;

        public string Feature { get; }
        public string Name { get; }

        /// <inheritdoc cref="OptionDefinition.Type"/>
        public Type Type => _optionDefinition.Type;

        /// <inheritdoc cref="OptionDefinition.DefaultValue"/>
        public T DefaultValue => (T)_optionDefinition.DefaultValue!;

        public ImmutableArray<OptionStorageLocation> StorageLocations { get; }

        public PerLanguageOption(string feature, string name, T defaultValue)
            : this(feature ?? throw new ArgumentNullException(nameof(feature)),
                   OptionGroup.Default,
                   name ?? throw new ArgumentNullException(nameof(name)),
                   defaultValue,
                   storageLocations: ImmutableArray<OptionStorageLocation>.Empty,
                   storageMapping: null,
                   isEditorConfigOption: false)
        {
        }

        public PerLanguageOption(string feature, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
            : this(feature ?? throw new ArgumentNullException(nameof(feature)),
                   OptionGroup.Default,
                   name ?? throw new ArgumentNullException(nameof(name)),
                   defaultValue,
                   PublicContract.RequireNonNullItems(storageLocations, nameof(storageLocations)).ToImmutableArray(),
                   storageMapping: null,
                   isEditorConfigOption: false)
        {
            // should not be used internally to create options
            Debug.Assert(storageLocations.All(l => l is not IEditorConfigValueSerializer));
        }

        private PerLanguageOption(
            string feature,
            OptionGroup group,
            string name,
            T defaultValue,
            ImmutableArray<OptionStorageLocation> storageLocations,
            OptionStorageMapping? storageMapping,
            bool isEditorConfigOption)
            : this(new OptionDefinition<T>(defaultValue, EditorConfigValueSerializer<T>.Unsupported, group, feature + "_" + name, storageMapping, isEditorConfigOption), feature, name, storageLocations)
        {
        }

        internal PerLanguageOption(OptionDefinition optionDefinition, string feature, string name, ImmutableArray<OptionStorageLocation> storageLocations)
        {
            Feature = feature;
            Name = name;
            _optionDefinition = optionDefinition;
            StorageLocations = storageLocations;
        }

        OptionDefinition IOption2.Definition => _optionDefinition;

        object? IOption.DefaultValue => this.DefaultValue;

        IPublicOption? IOption2.PublicOption => null;

        bool IOption.IsPerLanguage => true;

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
    }
}
