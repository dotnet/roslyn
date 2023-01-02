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

        /// <summary>
        /// Specifies mapping for internal options whose value is an aggregate of values of multiple public options.
        /// </summary>
        public InternalOptionStorageMapping? InternalStorageMapping { get; }

        public OptionDefinition(OptionGroup group, string configName, object? defaultValue, Type type, InternalOptionStorageMapping? internalStorageMapping, bool isEditorConfigOption)
        {
            ConfigName = configName;
            Group = group;
            DefaultValue = defaultValue;
            Type = type;
            InternalStorageMapping = internalStorageMapping;
            IsEditorConfigOption = isEditorConfigOption;
        }

        public OptionDefinition WithDefaultValue<T>(T defaultValue)
            => new(Group, ConfigName, defaultValue, typeof(T), InternalStorageMapping, IsEditorConfigOption);

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
    }
}
