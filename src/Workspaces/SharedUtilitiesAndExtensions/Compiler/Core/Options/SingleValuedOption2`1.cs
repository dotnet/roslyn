// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Marker interface for options that has the same value for all languages.
    /// </summary>
    internal interface ISingleValuedOption : IOptionWithGroup
    {
    }

    /// <inheritdoc cref="ISingleValuedOption"/>
    internal interface ISingleValuedOption<T> : ISingleValuedOption
    {
    }

    /// <summary>
    /// An global option. An instance of this class can be used to access an option value from an OptionSet.
    /// </summary>
    internal partial class SingleValuedOption2<T> : ISingleValuedOption<T>
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

        public SingleValuedOption2(string feature, string name, T defaultValue)
            : this(feature, name, defaultValue, storageLocations: ImmutableArray<OptionStorageLocation2>.Empty)
        {
        }

        public SingleValuedOption2(string feature, string name, T defaultValue, OptionStorageLocation2 storageLocation)
            : this(feature, group: OptionGroup.Default, name, defaultValue, ImmutableArray.Create(storageLocation))
        {
        }

        public SingleValuedOption2(string feature, string name, T defaultValue, ImmutableArray<OptionStorageLocation2> storageLocations)
            : this(feature, group: OptionGroup.Default, name, defaultValue, storageLocations)
        {
        }

        internal SingleValuedOption2(string feature, OptionGroup group, string name, T defaultValue)
            : this(feature, group, name, defaultValue, ImmutableArray<OptionStorageLocation2>.Empty)
        {
        }

        internal SingleValuedOption2(string feature, OptionGroup group, string name, T defaultValue, OptionStorageLocation2 storageLocation)
            : this(feature, group, name, defaultValue, ImmutableArray.Create(storageLocation))
        {
        }

        internal SingleValuedOption2(string feature, OptionGroup group, string name, T defaultValue, ImmutableArray<OptionStorageLocation2> storageLocations)
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

        public static implicit operator OptionKey2(SingleValuedOption2<T> option)
            => new(option);
    }
}
