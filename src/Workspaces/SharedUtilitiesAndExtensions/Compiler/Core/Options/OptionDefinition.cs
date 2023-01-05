// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    internal abstract class OptionDefinition : IEquatable<OptionDefinition?>
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
        /// True if the value of the option may be stored in an editorconfig file.
        /// </summary>
        public bool IsEditorConfigOption { get; }

        /// <summary>
        /// Specifies mapping for internal options whose value is an aggregate of values of multiple public options.
        /// </summary>
        public InternalOptionStorageMapping? InternalStorageMapping { get; }

        /// <summary>
        /// The untyped/boxed default value of the option.
        /// </summary>
        public object? DefaultValue { get; }

        public OptionDefinition(OptionGroup? group, string configName, object? defaultValue, InternalOptionStorageMapping? internalStorageMapping, bool isEditorConfigOption)
        {
            ConfigName = configName;
            Group = group ?? OptionGroup.Default;
            InternalStorageMapping = internalStorageMapping;
            IsEditorConfigOption = isEditorConfigOption;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// The type of the option value.
        /// </summary>
        public abstract Type Type { get; }

        public IEditorConfigValueSerializer Serializer => SerializerImpl;

        protected abstract IEditorConfigValueSerializer SerializerImpl { get; }

        public OptionDefinition<T> WithDefaultValue<T>(T defaultValue, EditorConfigValueSerializer<T> serializer)
            => new(defaultValue, serializer, Group, ConfigName, InternalStorageMapping, IsEditorConfigOption);

        public override bool Equals(object? other)
            => Equals(other as OptionDefinition);

        public bool Equals(OptionDefinition? other)
            => ConfigName == other?.ConfigName;

        public override int GetHashCode()
            => ConfigName.GetHashCode();

        public override string ToString()
            => ConfigName;

        public static bool operator ==(OptionDefinition? left, OptionDefinition? right)
            => ReferenceEquals(left, right) || left?.Equals(right) == true;

        public static bool operator !=(OptionDefinition? left, OptionDefinition? right)
            => !(left == right);
    }

    internal sealed class OptionDefinition<T> : OptionDefinition
    {
        public new T DefaultValue { get; }
        public new EditorConfigValueSerializer<T> Serializer { get; }

        public OptionDefinition(
            T defaultValue,
            EditorConfigValueSerializer<T>? serializer,
            OptionGroup? group,
            string configName,
            InternalOptionStorageMapping? internalStorageMapping,
            bool isEditorConfigOption)
            : base(group, configName, defaultValue, internalStorageMapping, isEditorConfigOption)
        {
            DefaultValue = defaultValue;
            Serializer = serializer ?? EditorConfigValueSerializer.Default<T>();
        }

        public override Type Type
            => typeof(T);

        protected override IEditorConfigValueSerializer SerializerImpl
            => Serializer;
    }
}
