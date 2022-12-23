// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Marker interface for options that has the same value for all languages.
    /// </summary>
    internal interface ISingleValuedOption : IOption2
    {
        /// <summary>
        /// The language name that supports this option, or null if it's supported by multiple languages.
        /// </summary>
        /// <remarks>
        /// This is an optional metadata used for:
        /// <list type="bullet">
        /// <item><description>Analyzer id to option mapping, used (for example) by configure code-style code action</description></item>
        /// <item><description>EditorConfig UI to determine whether to put this option under <c>[*.cs]</c>, <c>[*.vb]</c>, or <c>[*.{cs,vb}]</c></description></item>
        /// </list>
        /// Note that this property is not (and should not be) used for computing option values or storing options.
        /// </remarks>
        public string? LanguageName { get; }
    }

    /// <inheritdoc cref="ISingleValuedOption"/>
    internal interface ISingleValuedOption<T> : ISingleValuedOption
    {
    }

    internal partial class Option2<T> : ISingleValuedOption<T>
    {
        public OptionDefinition OptionDefinition { get; }

        /// <inheritdoc cref="OptionDefinition.Group"/>
        internal OptionGroup Group => OptionDefinition.Group;

        /// <inheritdoc cref="OptionDefinition.DefaultValue"/>
        public T DefaultValue => (T)OptionDefinition.DefaultValue!;

        /// <inheritdoc cref="OptionDefinition.Type"/>
        public Type Type => OptionDefinition.Type;

        public EditorConfigStorageLocation<T>? StorageLocation { get; }

        public Option2(string name, T defaultValue, EditorConfigStorageLocation<T>? storageLocation = null)
            : this(group: OptionGroup.Default, name: name, defaultValue: defaultValue, storageLocation: storageLocation)
        {
        }

        public Option2(OptionGroup group, string name, T defaultValue, EditorConfigStorageLocation<T>? storageLocation = null, string? languageName = null)
        {
            OptionDefinition = new OptionDefinition(group, name, defaultValue, typeof(T), isEditorConfigOption: storageLocation != null);
            StorageLocation = storageLocation;
            LanguageName = languageName;

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

            Debug.Assert(LanguageName is null == (OptionDefinition.ConfigName.StartsWith("dotnet_", StringComparison.Ordinal) ||
                OptionDefinition.ConfigName is "file_header_template" or "insert_final_newline"));
            Debug.Assert(LanguageName is LanguageNames.CSharp == OptionDefinition.ConfigName.StartsWith(OptionDefinition.CSharpConfigNamePrefix, StringComparison.Ordinal));
            Debug.Assert(LanguageName is LanguageNames.VisualBasic == OptionDefinition.ConfigName.StartsWith(OptionDefinition.VisualBasicConfigNamePrefix, StringComparison.Ordinal));
        }

        IEditorConfigStorageLocation? IOption2.StorageLocation => StorageLocation;

#if CODE_STYLE
        bool IOption2.IsPerLanguage => false;
#else
        string IOption.Feature => "config";
        string IOption.Name => OptionDefinition.ConfigName;
        object? IOption.DefaultValue => this.DefaultValue;
        bool IOption.IsPerLanguage => false;

        ImmutableArray<OptionStorageLocation> IOption.StorageLocations
            => (StorageLocation != null) ? ImmutableArray.Create((OptionStorageLocation)StorageLocation) : ImmutableArray<OptionStorageLocation>.Empty;
#endif

        OptionDefinition IOption2.OptionDefinition => OptionDefinition;

        public string? LanguageName { get; }

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

        public static implicit operator OptionKey2(Option2<T> option)
            => new(option);
    }
}
