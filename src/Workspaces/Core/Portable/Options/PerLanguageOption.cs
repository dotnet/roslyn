﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// An option that can be specified once per language.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PerLanguageOption<T> : IOption
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
        /// The type of the option value.
        /// </summary>
        public Type Type
        {
            get { return typeof(T); }
        }

        /// <summary>
        /// The default option value.
        /// </summary>
        public T DefaultValue { get; }

        public PerLanguageOption(string feature, string name, T defaultValue)
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

        Type IOption.Type
        {
            get { return typeof(T); }
        }

        object IOption.DefaultValue
        {
            get { return this.DefaultValue; }
        }

        bool IOption.IsPerLanguage
        {
            get { return true; }
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", this.Feature, this.Name);
        }
    }
}
