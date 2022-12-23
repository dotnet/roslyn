// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    [NonDefaultable]
    internal readonly struct OptionDefinition : IEquatable<OptionDefinition>
    {
        // Editorconfig name prefixes used for C#/VB specific options:
        public const string CSharpConfigNamePrefix = "csharp_";
        public const string VisualBasicConfigNamePrefix = "visual_basic_";

        private const string FeatureConfig = "config";

        private readonly string? _name;

        /// <summary>
        /// Feature this option is associated with. Obsolete.
        /// </summary>
        public string Feature { get; }

        /// <summary>
        /// Optional group/sub-feature for this option.
        /// </summary>
        internal OptionGroup Group { get; }

        /// <summary>
        /// A unique name of the option used in editorconfig.
        /// </summary>
        public string ConfigName { get; }

        /// <summary>
        /// The default value of the option.
        /// </summary>
        public object? DefaultValue { get; }

        /// <summary>
        /// The type of the option value.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// True if the value of the option may be stored in an editorconfig file.
        /// </summary>
        public bool IsEditorConfigOption { get; }

        public OptionDefinition(string? feature, OptionGroup group, string? name, string configName, object? defaultValue, Type type, bool isEditorConfigOption)
        {
            ConfigName = configName;
            Feature = feature ?? FeatureConfig;
            _name = name;
            Group = group;
            DefaultValue = defaultValue;
            Type = type;
            IsEditorConfigOption = isEditorConfigOption;
        }

        /// <summary>
        /// The legacy name of the option.
        /// </summary>
        public string Name => _name ?? ConfigName;

        public override bool Equals(object? obj)
            => obj is OptionDefinition key && Equals(key);

        public bool Equals(OptionDefinition other)
            => ConfigName == other.ConfigName;

        public override int GetHashCode()
            => ConfigName.GetHashCode();

        public override string ToString()
            => ConfigName;

        public static bool operator ==(OptionDefinition left, OptionDefinition right)
            => left.Equals(right);

        public static bool operator !=(OptionDefinition left, OptionDefinition right)
            => !left.Equals(right);

        #region Backward compat helpers

        // The following are used only to implement equality/ToString of public Option<T> and PerLanguageOption<T> options.
        // Public options can be instantiated with non-unique config name and thus we need to include default value in the equality
        // to avoid collisions among them.

        public string PublicOptionDefinitionToString()
            => $"{Feature} - {_name}";

        public bool PublicOptionDefinitionEquals(OptionDefinition other)
        {
            var equals = this.Name == other.Name &&
                this.Feature == other.Feature &&
                this.Group == other.Group;

            // DefaultValue and Type can differ between different but equivalent implementations of "ICodeStyleOption".
            // So, we skip these fields for equality checks of code style options.
            if (equals && !(this.DefaultValue is ICodeStyleOption))
            {
                equals = Equals(this.DefaultValue, other.DefaultValue) && this.Type == other.Type;
            }

            return equals;
        }

        #endregion
    }
}
