// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Marker interface for <see cref="Option{T}"/>
    /// </summary>
    internal interface ILanguageSpecificOption : IOptionWithGroup
    {
    }

    /// <summary>
    /// An global option. An instance of this class can be used to access an option value from an OptionSet.
    /// </summary>
    public class Option<T> : ILanguageSpecificOption
    {
        /// <summary>
        /// Feature this option is associated with.
        /// </summary>
        public string Feature { get; }

        /// <summary>
        /// Optional group/sub-feature for this option.
        /// </summary>
        internal OptionGroup Group { get; }

        /// <summary>
        /// The name of the option.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The default value of the option.
        /// </summary>
        public T DefaultValue { get; }

        /// <summary>
        /// The type of the option value.
        /// </summary>
        public Type Type => typeof(T);

        public ImmutableArray<OptionStorageLocation> StorageLocations { get; }

        public Option(string feature, string name)
            : this(feature, name, default)
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
        {
            if (string.IsNullOrWhiteSpace(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (group == null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            this.Feature = feature;
            this.Group = group;
            this.Name = name;
            this.DefaultValue = defaultValue;
            this.StorageLocations = storageLocations.ToImmutableArray();
        }

        object IOption.DefaultValue => this.DefaultValue;

        bool IOption.IsPerLanguage => false;

        OptionGroup IOptionWithGroup.Group => this.Group;

        public override string ToString()
        {
            return string.Format("{0} - {1}", this.Feature, this.Name);
        }

        public static implicit operator OptionKey(Option<T> option)
        {
            return new OptionKey(option);
        }
    }
}
