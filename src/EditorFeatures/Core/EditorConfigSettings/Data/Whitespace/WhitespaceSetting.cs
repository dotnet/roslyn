// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.EditorConfigSettings.Data.Whitespace;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract class WhitespaceSetting : IEditorConfigSettingInfo
    {
        // TODO: Switch to EditorConfig value holder: https://github.com/dotnet/roslyn/issues/63329
        protected static readonly ImmutableArray<string> _intValues = ImmutableArray.Create(new string[] { "2", "4", "8" });
        protected static readonly ImmutableArray<string> _newLineValues = ImmutableArray.Create(new string[] { "lf", "cr", "crlf" });
        protected static readonly ImmutableArray<string> _boolValues = ImmutableArray.Create(new string[] { "true", "false" });
        protected static readonly ImmutableArray<string> _spaceWithinParenthesesValues = ImmutableArray.Create(new string[] { "expressions", "type_casts", "control_flow_statements" });
        protected static readonly ImmutableArray<string> _newLinesForBracesValues = ImmutableArray.Create(new string[] { "all", "none", "accesors", "anonymous_methods", "anonymous_types", "control_blocks", "events", "indexers", "lambdas", "local_functions", "methods", "object_collection_array_initializers", "properties", "types" });

        protected OptionUpdater Updater { get; }
        protected string? Language { get; }

        protected WhitespaceSetting(string description, OptionUpdater updater, SettingLocation location, string? language = null)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Updater = updater;
            Location = location;
            Language = language;
        }

        public string Description { get; }
        public abstract string Category { get; }
        public abstract Type Type { get; }
        public abstract OptionKey2 Key { get; }
        public abstract void SetValue(object value);
        public abstract object? GetValue();
        public abstract bool IsDefinedInEditorConfig { get; }
        public abstract ImmutableArray<string>? GetSettingValues(OptionSet optionSet);

        public SettingLocation Location { get; protected set; }

        public static PerLanguageWhitespaceSetting<int> Create(PerLanguageOption2<int> option,
                                                                            string description,
                                                                            AnalyzerConfigOptions editorConfigOptions,
                                                                            OptionSet visualStudioOptions,
                                                                            OptionUpdater updater,
                                                                            string fileName)
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<int>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            return new PerLanguageIntegerWhitespaceSetting(option, description, editorConfigOptions, visualStudioOptions, updater, location);
        }

        public static PerLanguageWhitespaceSetting<bool> Create(PerLanguageOption2<bool> option,
                                                                            string description,
                                                                            AnalyzerConfigOptions editorConfigOptions,
                                                                            OptionSet visualStudioOptions,
                                                                            OptionUpdater updater,
                                                                            string fileName)
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<bool>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            return new PerLanguageBooleanWhitespaceSetting(option, description, editorConfigOptions, visualStudioOptions, updater, location);
        }

        public static PerLanguageWhitespaceSetting<string> Create(PerLanguageOption2<string> option,
                                                                            string description,
                                                                            AnalyzerConfigOptions editorConfigOptions,
                                                                            OptionSet visualStudioOptions,
                                                                            OptionUpdater updater,
                                                                            string fileName)
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<string>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            return new PerLanguageStringWhitespaceSetting(option, description, editorConfigOptions, visualStudioOptions, updater, location);
        }

        public static PerLanguageWhitespaceSetting<T> Create<T>(PerLanguageOption2<T> option,
                                                                            string description,
                                                                            AnalyzerConfigOptions editorConfigOptions,
                                                                            OptionSet visualStudioOptions,
                                                                            OptionUpdater updater,
                                                                            string fileName)
            where T : Enum
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<T>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            return new PerLanguageEnumWhitespaceSetting<T>(option, description, editorConfigOptions, visualStudioOptions, updater, location);
        }

        public static WhitespaceSetting<int> Create(Option2<int> option,
                                                    string description,
                                                    AnalyzerConfigOptions editorConfigOptions,
                                                    OptionSet visualStudioOptions,
                                                    OptionUpdater updater,
                                                    string fileName)
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<int>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            return new IntegerWhitespaceSetting(option, description, editorConfigOptions, visualStudioOptions, updater, location);
        }

        public static WhitespaceSetting<bool> Create(Option2<bool> option,
                                                    string description,
                                                    AnalyzerConfigOptions editorConfigOptions,
                                                    OptionSet visualStudioOptions,
                                                    OptionUpdater updater,
                                                    string fileName)
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<bool>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            return new BooleanWhitespaceSetting(option, description, editorConfigOptions, visualStudioOptions, updater, location);
        }

        public static WhitespaceSetting<string> Create(Option2<string> option,
                                                    string description,
                                                    AnalyzerConfigOptions editorConfigOptions,
                                                    OptionSet visualStudioOptions,
                                                    OptionUpdater updater,
                                                    string fileName)
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<string>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            return new StringWhitespaceSetting(option, description, editorConfigOptions, visualStudioOptions, updater, location);
        }

        public static WhitespaceSetting<T> Create<T>(Option2<T> option,
                                                    string description,
                                                    AnalyzerConfigOptions editorConfigOptions,
                                                    OptionSet visualStudioOptions,
                                                    OptionUpdater updater,
                                                    string fileName)
            where T : Enum
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<T>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            return new EnumWhitespaceSetting<T>(option, description, editorConfigOptions, visualStudioOptions, updater, location);
        }

        internal IEditorConfigStorageLocation2? GetEditorConfigStorageLocation()
        {
            return Key.Option.StorageLocations.OfType<IEditorConfigStorageLocation2>().FirstOrDefault();
        }

        public string? GetSettingName()
        {
            var storageLocation = GetEditorConfigStorageLocation();
            return storageLocation?.KeyName;
        }

        public string GetDocumentation()
        {
            return Description;
        }

        protected static ImmutableArray<string>? GetEnumSettingValuesHelper(IEditorConfigStorageLocation2? storageLocation, Type type, OptionSet optionSet)
        {
            // TODO: Switch to EditorConfig value holder: https://github.com/dotnet/roslyn/issues/63329
            var strings = new List<string>();
            var enumValues = type.GetEnumValues();

            foreach (var enumValue in enumValues)
            {
                var option = storageLocation?.GetEditorConfigStringValue(enumValue, optionSet);
                if (option != null)
                {
                    strings.Add(option);
                }
            }

            return strings.ToImmutableArray();
        }

        protected static ImmutableArray<string>? GetStringSettingValuesHelper(IEditorConfigStorageLocation2? storageLocation)
        {
            // TODO: Switch to EditorConfig value holder: https://github.com/dotnet/roslyn/issues/63329
            var strings = new List<string>();
            if (storageLocation?.KeyName == "end_of_line")
            {
                _newLineValues.Do(strings.Add);
                return strings.ToImmutableArray();
            }
            return null;
        }

        protected static ImmutableArray<string>? GetBooleanSettingValuesHelper(IEditorConfigStorageLocation2? storageLocation)
        {
            // TODO: Switch to EditorConfig value holder: https://github.com/dotnet/roslyn/issues/63329
            var strings = new List<string>();
            if (storageLocation?.KeyName == "csharp_new_line_before_open_brace")
            {
                _newLinesForBracesValues.Do(strings.Add);
                return strings.ToImmutableArray();
            }

            if (storageLocation?.KeyName == "csharp_space_between_parentheses")
            {
                _spaceWithinParenthesesValues.Do(strings.Add);
                return strings.ToImmutableArray();
            }

            return _boolValues;
        }
    }
}
