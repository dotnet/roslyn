// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Marker interface for <see cref="PerLanguageOption{T}"/>
    /// </summary>
    internal interface IPerLanguageOption : IOptionWithGroup
    {
    }

    /// <summary>
    /// An option that can be specified once per language.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PerLanguageOption<T> : IPerLanguageOption
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
        /// The type of the option value.
        /// </summary>
        public Type Type => typeof(T);

        /// <summary>
        /// The default option value.
        /// </summary>
        public T DefaultValue { get; }

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

        bool IOption.IsPerLanguage => true;

        OptionGroup IOptionWithGroup.Group => this.Group;

        public override string ToString()
        {
            return string.Format("{0} - {1}", this.Feature, this.Name);
        }
    }
}
