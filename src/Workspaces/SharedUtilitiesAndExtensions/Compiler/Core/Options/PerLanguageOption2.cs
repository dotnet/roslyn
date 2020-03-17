// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;

#if !CODE_STYLE
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Marker interface for <see cref="PerLanguageOption2{T}"/>
    /// </summary>
    internal interface IPerLanguageOption : IOptionWithGroup
    {
    }

    /// <summary>
    /// Marker interface for <see cref="PerLanguageOption2{T}"/>
    /// </summary>
    internal interface IPerLanguageOption<T> : IPerLanguageOption
    {
    }

    /// <summary>
    /// An option that can be specified once per language.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class PerLanguageOption2<T> : IPerLanguageOption<T>
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

        public ImmutableArray<OptionStorageLocation2> StorageLocations { get; }

        public PerLanguageOption2(string feature, string name, T defaultValue)
            : this(feature, name, defaultValue, storageLocations: Array.Empty<OptionStorageLocation2>())
        {
        }

        public PerLanguageOption2(string feature, string name, T defaultValue, params OptionStorageLocation2[] storageLocations)
            : this(feature, group: OptionGroup.Default, name, defaultValue, storageLocations)
        {
        }

        internal PerLanguageOption2(string feature, OptionGroup group, string name, T defaultValue, params OptionStorageLocation2[] storageLocations)
            : this(feature, group, name, defaultValue, storageLocations.ToImmutableArray())
        {
        }

        internal PerLanguageOption2(string feature, OptionGroup group, string name, T defaultValue, ImmutableArray<OptionStorageLocation2> storageLocations)
        {
            if (string.IsNullOrWhiteSpace(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            _optionDefinition = new OptionDefinition(feature, group, name, defaultValue, typeof(T), isPerLanguage: true);
            this.StorageLocations = storageLocations;
        }

        OptionGroup IOptionWithGroup.Group => this.Group;

        OptionDefinition IOption2.OptionDefinition => _optionDefinition;

#if CODE_STYLE
        object? IOption2.DefaultValue => this.DefaultValue;

        bool IOption2.IsPerLanguage => true;
#else
        object? IOption.DefaultValue => this.DefaultValue;

        bool IOption.IsPerLanguage => true;

        ImmutableArray<OptionStorageLocation> IOption.StorageLocations
            => this.StorageLocations.As<OptionStorageLocation>();
#endif
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

#if !CODE_STYLE
        public static implicit operator PerLanguageOption<T>(PerLanguageOption2<T> option)
        {
            RoslynDebug.Assert(option != null);

            return new PerLanguageOption<T>(option._optionDefinition, option.StorageLocations.As<OptionStorageLocation>());
        }
#endif
    }
}
