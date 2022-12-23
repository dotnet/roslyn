// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Marker interface for <see cref="PerLanguageOption2{T}"/>.
    /// This option may apply to multiple languages, such that the option can have a different value for each language.
    /// </summary>
    internal interface IPerLanguageValuedOption : IOption2
    {
    }

    /// <inheritdoc cref="IPerLanguageValuedOption"/>
    internal interface IPerLanguageValuedOption<T> : IPerLanguageValuedOption
    {
    }

    /// <summary>
    /// An option that can be specified once per language.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal partial class PerLanguageOption2<T> : IPerLanguageValuedOption<T>
    {
        public OptionDefinition OptionDefinition { get; }

        /// <inheritdoc cref="OptionDefinition.Group"/>
        internal OptionGroup Group => OptionDefinition.Group;

        /// <inheritdoc cref="OptionDefinition.Type"/>
        public Type Type => OptionDefinition.Type;

        /// <inheritdoc cref="OptionDefinition.DefaultValue"/>
        public T DefaultValue => (T)OptionDefinition.DefaultValue!;

        public EditorConfigStorageLocation<T>? StorageLocation { get; }

        public PerLanguageOption2(string name, T defaultValue, EditorConfigStorageLocation<T>? storageLocation = null)
            : this(OptionGroup.Default, name, defaultValue, storageLocation)
        {
        }

        public PerLanguageOption2(OptionGroup group, string name, T defaultValue, EditorConfigStorageLocation<T>? storageLocation = null)
        {
            Debug.Assert(storageLocation == null || storageLocation.KeyName == name);

            var isEditorConfigOption = storageLocation != null || typeof(T) == typeof(NamingStylePreferences);
            OptionDefinition = new OptionDefinition(group, name, defaultValue, typeof(T), isEditorConfigOption);
            StorageLocation = storageLocation;

            VerifyNamingConvention();
        }

        [Conditional("DEBUG")]
        private void VerifyNamingConvention()
        {
            // TODO: remove, once all options have editorconfig-like name https://github.com/dotnet/roslyn/issues/65787
            if (StorageLocation is null)
            {
                return;
            }

            // options with per-language values shouldn't have language-specific prefix
            Debug.Assert(!OptionDefinition.ConfigName.StartsWith(OptionDefinition.CSharpConfigNamePrefix, StringComparison.Ordinal));
            Debug.Assert(!OptionDefinition.ConfigName.StartsWith(OptionDefinition.VisualBasicConfigNamePrefix, StringComparison.Ordinal));
        }

        OptionDefinition IOption2.OptionDefinition => OptionDefinition;
        IEditorConfigStorageLocation? IOption2.StorageLocation => StorageLocation;

#if CODE_STYLE
        bool IOption2.IsPerLanguage => true;
#else
        string IOption.Feature => "config";
        string IOption.Name => OptionDefinition.ConfigName;
        object? IOption.DefaultValue => this.DefaultValue;
        bool IOption.IsPerLanguage => true;

        ImmutableArray<OptionStorageLocation> IOption.StorageLocations
            => (StorageLocation != null) ? ImmutableArray.Create((OptionStorageLocation)StorageLocation) : ImmutableArray<OptionStorageLocation>.Empty;
#endif
        public override string ToString() => OptionDefinition.ToString();

        public override int GetHashCode() => OptionDefinition.GetHashCode();

        public override bool Equals(object? obj) => Equals(obj as IOption2);

        public bool Equals(IOption2? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return OptionDefinition == other?.OptionDefinition;
        }
    }
}
