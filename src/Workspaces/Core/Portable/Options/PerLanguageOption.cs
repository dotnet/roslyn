// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    /// <inheritdoc cref="PerLanguageOption2{T}"/>
    public class PerLanguageOption<T> : IPerLanguageOption<T>
    {
        private readonly OptionDefinition _optionDefinition;

        /// <summary>
        /// Feature this option is associated with.
        /// </summary>
        public string Feature => _optionDefinition.Feature;

        /// <summary>
        /// Optional group/sub-feature for this option.
        /// </summary>
        internal OptionGroup Group => _optionDefinition.Group;

        /// <summary>
        /// The name of the option.
        /// </summary>
        public string Name => _optionDefinition.Name;

        /// <summary>
        /// The type of the option value.
        /// </summary>
        public Type Type => _optionDefinition.Type;

        /// <summary>
        /// The default option value.
        /// </summary>
        public T DefaultValue => (T)_optionDefinition.DefaultValue!;

        public ImmutableArray<OptionStorageLocation> StorageLocations { get; }

        public PerLanguageOption(string feature, string name, T defaultValue)
            : this(feature, name, defaultValue, storageLocations: Array.Empty<OptionStorageLocation>())
        {
        }

        public PerLanguageOption(string feature, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
            : this(feature, group: OptionGroup.Default, name, defaultValue, storageLocations)
        {
        }

        internal PerLanguageOption(string feature, OptionGroup group, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
            : this(feature, group, name, defaultValue, storageLocations.ToImmutableArray())
        {
        }

        internal PerLanguageOption(string feature, OptionGroup group, string name, T defaultValue, ImmutableArray<OptionStorageLocation> storageLocations)
            : this(new OptionDefinition(feature, group, name, defaultValue, typeof(T), isPerLanguage: true), storageLocations)
        {
            if (string.IsNullOrWhiteSpace(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }
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

        public override string ToString() => _optionDefinition.ToString();

        public override int GetHashCode()
            => _optionDefinition.GetHashCode();

        bool IEquatable<IOption2?>.Equals(IOption2? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return _optionDefinition == other?.OptionDefinition;
        }
    }
}
