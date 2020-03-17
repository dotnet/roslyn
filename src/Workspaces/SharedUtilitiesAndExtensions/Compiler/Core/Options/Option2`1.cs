// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;

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
    internal class Option2<T> : ILanguageSpecificOption<T>
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
        /// The default value of the option.
        /// </summary>
        public T DefaultValue => (T)_optionDefinition.DefaultValue!;

        /// <summary>
        /// The type of the option value.
        /// </summary>
        public Type Type => _optionDefinition.Type;

        public ImmutableArray<OptionStorageLocation2> StorageLocations { get; }

        [Obsolete("Use a constructor that specifies an explicit default value.")]
        public Option2(string feature, string name)
            : this(feature, name, default!)
        {
            // This constructor forwards to the next one; it exists to maintain source-level compatibility with older callers.
        }

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

            _optionDefinition = new OptionDefinition(feature, group, name, defaultValue, typeof(T), isPerLanguage: false);
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

        OptionDefinition IOption2.OptionDefinition => _optionDefinition;

        public override string ToString() => _optionDefinition.ToString();

        public override int GetHashCode() => _optionDefinition.GetHashCode();

        public override bool Equals(object? obj) => Equals(obj as IOption2);

        public bool Equals(IOption2? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return _optionDefinition == other?.OptionDefinition;
        }

        public static implicit operator OptionKey2(Option2<T> option)
        {
            return new OptionKey2(option);
        }

#if !CODE_STYLE
        public static implicit operator Option<T>(Option2<T> option)
        {
            RoslynDebug.Assert(option != null);

            return new Option<T>(option._optionDefinition, option.StorageLocations.As<OptionStorageLocation>());
        }
#endif
    }
}
