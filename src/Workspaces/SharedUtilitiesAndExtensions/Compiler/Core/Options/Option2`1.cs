// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Marker interface for language specific options.
    /// </summary>
    internal interface ILanguageSpecificOption : IOptionWithGroup
    {
    }

    /// <summary>
    /// Marker interface for language specific options.
    /// </summary>
    internal interface ILanguageSpecificOption<T> : ILanguageSpecificOption
    {
    }

    /// <summary>
    /// An global option. An instance of this class can be used to access an option value from an OptionSet.
    /// </summary>
    internal partial class Option2<T> : ILanguageSpecificOption<T>
    {
        public OptionDefinition OptionDefinition { get; }

        /// <inheritdoc cref="OptionDefinition.Feature"/>
        public string Feature => OptionDefinition.Feature;

        /// <inheritdoc cref="OptionDefinition.Group"/>
        internal OptionGroup Group => OptionDefinition.Group;

        /// <inheritdoc cref="OptionDefinition.Name"/>
        public string Name => OptionDefinition.Name;

        /// <inheritdoc cref="OptionDefinition.DefaultValue"/>
        public T DefaultValue => (T)OptionDefinition.DefaultValue!;

        /// <inheritdoc cref="OptionDefinition.Type"/>
        public Type Type => OptionDefinition.Type;

        /// <summary>
        /// Storage locations for the option.
        /// </summary>
        public ImmutableArray<OptionStorageLocation2> StorageLocations { get; }

        public Option2(string feature, string name, T defaultValue)
            : this(feature, name, defaultValue, storageLocations: Array.Empty<OptionStorageLocation2>())
        {
        }

        public Option2(string feature, string name, T defaultValue, params OptionStorageLocation2[] storageLocations)
            : this(feature, group: OptionGroup.Default, name, defaultValue, storageLocations)
        {
        }

        internal Option2(string feature, OptionGroup group, string name, T defaultValue, params OptionStorageLocation2[] storageLocations)
            : this(feature, group, name, defaultValue, storageLocations.ToImmutableArray())
        {
        }

        internal Option2(string feature, OptionGroup group, string name, T defaultValue, ImmutableArray<OptionStorageLocation2> storageLocations)
        {
            if (string.IsNullOrWhiteSpace(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            OptionDefinition = new OptionDefinition(feature, group, name, defaultValue, typeof(T), isPerLanguage: false);
            this.StorageLocations = storageLocations;
        }

#if CODE_STYLE
        object? IOption2.DefaultValue => this.DefaultValue;

        bool IOption2.IsPerLanguage => false;
#else
        object? IOption.DefaultValue => this.DefaultValue;

        bool IOption.IsPerLanguage => false;

        ImmutableArray<OptionStorageLocation> IOption.StorageLocations
            => this.StorageLocations.As<OptionStorageLocation>();
#endif

        OptionGroup IOptionWithGroup.Group => this.Group;

        OptionDefinition IOption2.OptionDefinition => OptionDefinition;

        public override string ToString() => OptionDefinition.ToString();

        public override int GetHashCode() => OptionDefinition.GetHashCode();

        public override bool Equals(object? obj) => Equals(obj as IOption2);

        public bool Equals(IOption2? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return OptionDefinition == other?.OptionDefinition;
        }

        public static implicit operator OptionKey2(Option2<T> option)
            => new OptionKey2(option);
    }
}
