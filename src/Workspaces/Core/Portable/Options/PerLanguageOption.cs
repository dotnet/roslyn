// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// An option that can be specified once per language.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PerLanguageOption<T> : IOption2, IOption
    {
        /// <summary>
        /// Save per language defaults
        /// </summary>
        private readonly IImmutableDictionary<string, T> _perLanguageDefaults;

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

        /// <summary>
        /// The default option value of specific language
        /// </summary>
        internal T GetDefaultValue(string language)
        {
            if (_perLanguageDefaults.Count == 0)
            {
                return DefaultValue;
            }

            T languageSpecificDefault;
            if (!_perLanguageDefaults.TryGetValue(language, out languageSpecificDefault))
            {
                return DefaultValue;
            }

            return languageSpecificDefault;
        }

        public PerLanguageOption(string feature, string name, T defaultValue) :
            this(feature, name, defaultValue, ImmutableDictionary<string, T>.Empty)
        {
        }

        internal PerLanguageOption(
            string feature, string name, T defaultValue, IDictionary<string, T> perLanguageDefaults)
        {
            if (string.IsNullOrWhiteSpace(feature))
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            if (perLanguageDefaults == null)
            {
                throw new ArgumentNullException(nameof(perLanguageDefaults));
            }

            this.Feature = feature;
            this.Name = name;
            this.DefaultValue = defaultValue;

            this._perLanguageDefaults =
                (perLanguageDefaults as IImmutableDictionary<string, T>) ??
                        ImmutableDictionary.CreateRange<string, T>(perLanguageDefaults);
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

        object IOption2.GetDefaultValue(string language)
        {
            return this.GetDefaultValue(language);
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", this.Feature, this.Name);
        }
    }
}
