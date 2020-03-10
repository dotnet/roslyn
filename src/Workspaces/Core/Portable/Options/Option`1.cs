// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// An global option. An instance of this class can be used to access an option value from an OptionSet.
    /// </summary>
    public class Option<T> : ILanguageSpecificOption<T>
    {
        private readonly Option2<T> _optionImpl;

        /// <summary>
        /// Feature this option is associated with.
        /// </summary>
        public string Feature => _optionImpl.Feature;

        /// <summary>
        /// Optional group/sub-feature for this option.
        /// </summary>
        internal OptionGroup Group => _optionImpl.Group;

        /// <summary>
        /// The name of the option.
        /// </summary>
        public string Name => _optionImpl.Name;

        /// <summary>
        /// The default value of the option.
        /// </summary>
        public T DefaultValue => _optionImpl.DefaultValue;

        /// <summary>
        /// The type of the option value.
        /// </summary>
        public Type Type => _optionImpl.Type;

        public ImmutableArray<OptionStorageLocation> StorageLocations { get; }

        [Obsolete("Use a constructor that specifies an explicit default value.")]
        public Option(string feature, string name)
            : this(feature, name, default!)
        {
            // This constructor forwards to the next one; it exists to maintain source-level compatibility with older callers.
        }

        public Option(string feature, string name, T defaultValue)
            : this(feature, name, defaultValue, storageLocations: Array.Empty<OptionStorageLocation>())
        {
        }

        public Option(string feature, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
            : this(feature, group: OptionGroup.Default, name, defaultValue, storageLocations)
        {
        }

        internal Option(string feature, OptionGroup group, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
            : this(feature, group, name, defaultValue, storageLocations.ToImmutableArray())
        {
        }

        internal Option(string feature, OptionGroup group, string name, T defaultValue, ImmutableArray<OptionStorageLocation> storageLocations)
        {
            if (string.IsNullOrWhiteSpace(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            _optionImpl = new Option2<T>(feature, group, name, defaultValue);
            this.StorageLocations = storageLocations;
        }

        OptionGroup IOptionWithGroup.Group => this.Group;

        object? IOption.DefaultValue => this.DefaultValue;

        bool IOption.IsPerLanguage => false;

        public override string ToString() => _optionImpl.ToString();

        public static implicit operator OptionKey(Option<T> option)
        {
            return new OptionKey(option);
        }
    }
}
