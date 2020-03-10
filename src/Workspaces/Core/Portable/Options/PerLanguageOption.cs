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
        private readonly PerLanguageOption2<T> _perLanguageOptionImpl;

        /// <summary>
        /// Feature this option is associated with.
        /// </summary>
        public string Feature => _perLanguageOptionImpl.Feature;

        /// <summary>
        /// Optional group/sub-feature for this option.
        /// </summary>
        internal OptionGroup Group => _perLanguageOptionImpl.Group;

        /// <summary>
        /// The name of the option.
        /// </summary>
        public string Name => _perLanguageOptionImpl.Name;

        /// <summary>
        /// The type of the option value.
        /// </summary>
        public Type Type => _perLanguageOptionImpl.Type;

        /// <summary>
        /// The default option value.
        /// </summary>
        public T DefaultValue => _perLanguageOptionImpl.DefaultValue;

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
        {
            if (string.IsNullOrWhiteSpace(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            _perLanguageOptionImpl = new PerLanguageOption2<T>(feature, group, name, defaultValue);
            StorageLocations = storageLocations;
        }

        OptionGroup IOptionWithGroup.Group => this.Group;

        object? IOption.DefaultValue => this.DefaultValue;

        bool IOption.IsPerLanguage => true;

        public override string ToString() => _perLanguageOptionImpl.ToString();
    }
}
