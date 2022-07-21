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

    /// <summary>
    /// An global option. An instance of this class can be used to access an option value from an OptionSet.
    /// </summary>
    internal partial class Option2<T> : ISingleValuedOption<T>
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
            : this(feature, group: OptionGroup.Default, name, defaultValue, storageLocations: ImmutableArray<OptionStorageLocation2>.Empty)
        {
        }

        public Option2(string feature, string name, T defaultValue, OptionStorageLocation2 storageLocation)
            : this(feature, group: OptionGroup.Default, name, defaultValue, ImmutableArray.Create(storageLocation))
        {
        }

        public Option2(string feature, OptionGroup group, string name, T defaultValue, OptionStorageLocation2 storageLocation)
            : this(feature, group, name, defaultValue, ImmutableArray.Create(storageLocation))
        {
        }

        public Option2(string feature, OptionGroup group, string name, T defaultValue, ImmutableArray<OptionStorageLocation2> storageLocations)
            : this(feature, group, name, defaultValue, storageLocations, null)
        {
        }

        public Option2(string feature, OptionGroup group, string name, T defaultValue, ImmutableArray<OptionStorageLocation2> storageLocations, string? languageName)
        {
            if (string.IsNullOrWhiteSpace(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            OptionDefinition = new OptionDefinition(feature, group, name, defaultValue, typeof(T));
            this.StorageLocations = storageLocations;
            LanguageName = languageName;
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

        public string? LanguageName { get; }

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
            => new(option);
    }
}
