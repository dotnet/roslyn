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
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract class WhitespaceSetting : IEditorConfigSettingInfo
    {
        private static readonly ImmutableArray<string> _boolValues = ImmutableArray.Create(new string[] { "true", "false" });
        private static readonly ImmutableArray<string> _intValues = ImmutableArray.Create(new string[] { "2", "4", "8" });
        private static readonly ImmutableArray<string> _spaceWithinParenthesesValues = ImmutableArray.Create(new string[] { "expressions", "type_casts", "control_flow_statements" });
        private static readonly ImmutableArray<string> _newLinesForBracesValues = ImmutableArray.Create(new string[] { "all", "none", "accesors", "anonymous_methods", "anonymous_types", "control_blocks", "events", "indexers", "lambdas", "local_functions", "methods", "object_collection_array_initializers", "properties", "types" });

        protected OptionUpdater Updater { get; }
        protected string? Language { get; }

        protected WhitespaceSetting(string description, OptionUpdater updater, SettingLocation location, ImmutableDictionary<string, string>? valuesDescription = null, string? language = null)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Updater = updater;
            Location = location;
            Language = language;
            ValuesDescription = valuesDescription;
        }

        public string Description { get; }
        public abstract string Category { get; }
        public abstract Type Type { get; }
        public abstract OptionKey2 Key { get; }
        public abstract void SetValue(object value);
        public abstract object? GetValue();
        public abstract bool IsDefinedInEditorConfig { get; }
        public SettingLocation Location { get; protected set; }
        public ImmutableDictionary<string, string>? ValuesDescription { get; }

        public static PerLanguageWhitespaceSetting<TOption> Create<TOption>(PerLanguageOption2<TOption> option,
                                                                            string description,
                                                                            AnalyzerConfigOptions editorConfigOptions,
                                                                            OptionSet visualStudioOptions,
                                                                            OptionUpdater updater,
                                                                            string fileName,
                                                                            ImmutableDictionary<string, string>? valuesDescription = null)
            where TOption : notnull
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<TOption>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            return new PerLanguageWhitespaceSetting<TOption>(option, description, editorConfigOptions, visualStudioOptions, updater, location, valuesDescription);
        }

        public static WhitespaceSetting<TOption> Create<TOption>(Option2<TOption> option,
                                                                 string description,
                                                                 AnalyzerConfigOptions editorConfigOptions,
                                                                 OptionSet visualStudioOptions,
                                                                 OptionUpdater updater,
                                                                 string fileName,
                                                                 ImmutableDictionary<string, string>? valuesDescription = null)
            where TOption : struct
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<TOption>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            return new WhitespaceSetting<TOption>(option, description, editorConfigOptions, visualStudioOptions, updater, location, valuesDescription);
        }

        private IEditorConfigStorageLocation2? GetEditorConfigStorageLocation()
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

        public ImmutableArray<string>? GetSettingValues(OptionSet optionSet)
        {
            var storageLocation = GetEditorConfigStorageLocation();
            var type = Key.Option.DefaultValue?.GetType();

            if (storageLocation?.KeyName == "csharp_new_line_before_open_brace")
            {
                var strings = new List<string>();

                _newLinesForBracesValues.Do(strings.Add);

                return strings.ToImmutableArray();
            }

            if (storageLocation?.KeyName == "csharp_space_between_parentheses")
            {
                var strings = new List<string>();

                _spaceWithinParenthesesValues.Do(strings.Add);

                return strings.ToImmutableArray();
            }

            if (type == null)
            {
                return null;
            }

            if (type == typeof(bool))
            {
                return _boolValues;
            }
            if (type == typeof(int))
            {
                return _intValues;

            }
            if (type.BaseType == typeof(Enum))
            {
                var strings = new List<string>();
                var enumValues = type.GetEnumValues();

                foreach (var enumValue in enumValues)
                {
                    if (enumValue != null)
                    {
                        var option = storageLocation?.GetEditorConfigStringValue(enumValue, optionSet);
                        if (option != null)
                        {
                            strings.Add(option);
                        }
                    }
                }

                return strings.ToImmutableArray();
            }

            return null;
        }

        public string? GetValueDocumentation(string value)
        {
            if (ValuesDescription != null)
            {
                return ValuesDescription[value];
            }

            return null;
        }
    }
}
