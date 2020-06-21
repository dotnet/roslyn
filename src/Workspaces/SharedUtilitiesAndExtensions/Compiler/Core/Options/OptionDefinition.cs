// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    [NonDefaultable]
    internal readonly struct OptionDefinition : IEquatable<OptionDefinition>
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
        public object? DefaultValue { get; }

        /// <summary>
        /// The type of the option value.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Flag indicating if this is a per-language option or a language specific option.
        /// </summary>
        public bool IsPerLanguage { get; }

        public OptionDefinition(string feature, OptionGroup group, string name, object? defaultValue, Type type, bool isPerLanguage)
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
            this.Group = group ?? throw new ArgumentNullException(nameof(group));
            this.Name = name;
            this.DefaultValue = defaultValue;
            this.Type = type ?? throw new ArgumentNullException(nameof(type));
            this.IsPerLanguage = isPerLanguage;
        }

        public override bool Equals(object? obj)
        {
            return obj is OptionDefinition key &&
                   Equals(key);
        }

        public bool Equals(OptionDefinition other)
        {
            var equals = this.Name == other.Name &&
                this.Feature == other.Feature &&
                this.Group == other.Group &&
                this.IsPerLanguage == other.IsPerLanguage;

            // DefaultValue and Type can differ between different but equivalent implementations of "ICodeStyleOption".
            // So, we skip these fields for equality checks of code style options.
            if (equals && !(this.DefaultValue is ICodeStyleOption))
            {
                equals = Equals(this.DefaultValue, other.DefaultValue) && this.Type == other.Type;
            }

            return equals;
        }

        public override int GetHashCode()
        {
            var hash = this.Feature.GetHashCode();
            hash = unchecked((hash * (int)0xA5555529) + this.Group.GetHashCode());
            hash = unchecked((hash * (int)0xA5555529) + this.Name.GetHashCode());
            hash = unchecked((hash * (int)0xA5555529) + this.IsPerLanguage.GetHashCode());

            // DefaultValue and Type can differ between different but equivalent implementations of "ICodeStyleOption".
            // So, we skip these fields for hash computation of code style options.
            if (!(this.DefaultValue is ICodeStyleOption))
            {
                hash = unchecked((hash * (int)0xA5555529) + this.DefaultValue?.GetHashCode() ?? 0);
                hash = unchecked((hash * (int)0xA5555529) + this.Type.GetHashCode());
            }

            return hash;
        }

        public override string ToString()
            => string.Format("{0} - {1}", this.Feature, this.Name);

        public static bool operator ==(OptionDefinition left, OptionDefinition right)
            => left.Equals(right);

        public static bool operator !=(OptionDefinition left, OptionDefinition right)
            => !left.Equals(right);
    }
}
