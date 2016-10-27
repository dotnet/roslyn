// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// An global option. An instance of this class can be used to access an option value from an OptionSet.
    /// </summary>
    public class Option<T> : IOption
    {
        /// <summary>
        /// Feature this option is associated with.
        /// </summary>
        public string Feature { get; }

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
            : this(feature, name, default(T))
        {
            // This constructor forwards to the next one; it exists to maintain source-level compatibility with older callers.
        }

        public Option(string feature, string name, T defaultValue)
        {
            if (string.IsNullOrWhiteSpace(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            this.Feature = feature;
            this.Name = name;
            this.DefaultValue = defaultValue;
        }

        public Option(string feature, string name, T defaultValue, params OptionStorageLocation[] storageLocations)
            : this(feature, name, defaultValue)
        {
            StorageLocations = storageLocations.ToImmutableArray();
        }

        object IOption.DefaultValue => this.DefaultValue;

        bool IOption.IsPerLanguage => false;

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
