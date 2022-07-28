// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract class WhitespaceSetting : IEditorConfigSettingInfo
    {
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
        public SettingLocation Location { get; protected set; }

        public static PerLanguageWhitespaceSetting<TOption> Create<TOption>(PerLanguageOption2<TOption> option,
                                                                            string description,
                                                                            AnalyzerConfigOptions editorConfigOptions,
                                                                            OptionSet visualStudioOptions,
                                                                            OptionUpdater updater,
                                                                            string fileName)
            where TOption : notnull
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<TOption>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            return new PerLanguageWhitespaceSetting<TOption>(option, description, editorConfigOptions, visualStudioOptions, updater, location);
        }

        public static WhitespaceSetting<TOption> Create<TOption>(Option2<TOption> option,
                                                                 string description,
                                                                 AnalyzerConfigOptions editorConfigOptions,
                                                                 OptionSet visualStudioOptions,
                                                                 OptionUpdater updater,
                                                                 string fileName)
            where TOption : struct
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<TOption>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            return new WhitespaceSetting<TOption>(option, description, editorConfigOptions, visualStudioOptions, updater, location);
        }

        public string GetSettingName()
        {
            return ((IEditorConfigStorageLocation2)Key.Option.StorageLocations.First()).KeyName;
        }

        public string GetDocumentation()
        {
            return Description;
        }

        public string[]? GetSettingValues(OptionSet optionSet)
        {
            var type = Key.Option.DefaultValue?.GetType();
            if (type == null)
            {
                return null;
            }

            if (type.Name == "Boolean")
            {
                return new string[] { "true", "false" };
            }
            if (type.Name == "Int32")
            {
                return new string[] { "2", "4", "6", "8" };
            }
            if (type.BaseType?.Name == "Enum")
            {
                var strings = new List<string>();
                var enumValues = type.GetEnumValues();

                foreach (var enumValue in enumValues)
                {
                    if (enumValue != null)
                    {
                        var option = ((IEditorConfigStorageLocation2)Key.Option.StorageLocations.First()).GetEditorConfigStringValue(enumValue, optionSet);
                        strings.Add(option);
                    }
                }

                return strings.ToArray();
            }

            return null;
        }
    }
}
