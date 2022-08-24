// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.EditorConfigSettings;
using Microsoft.CodeAnalysis.EditorConfigSettings.Data.Whitespace;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract class WhitespaceSetting : IEditorConfigSettingInfo
    {
        protected OptionUpdater Updater { get; }
        protected string? Language { get; }

        private readonly IEditorConfigData EditorConfigData;

        protected WhitespaceSetting(string description, OptionUpdater updater, SettingLocation location, IEditorConfigData editorConfigData, ImmutableDictionary<string, string>? valuesDocumentation = null, string? language = null)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Updater = updater;
            Location = location;
            Language = language;
            ValuesDocumentation = valuesDocumentation;
            EditorConfigData = editorConfigData;
        }

        public string Description { get; }
        public abstract string Category { get; }
        public abstract Type Type { get; }
        public abstract OptionKey2 Key { get; }
        public abstract void SetValue(object value);
        public abstract object? GetValue();
        public abstract bool IsDefinedInEditorConfig { get; }

        public SettingLocation Location { get; protected set; }
        public ImmutableDictionary<string, string>? ValuesDocumentation { get; }

        public static PerLanguageWhitespaceSetting<int> Create(PerLanguageOption2<int> option,
                                                                            AnalyzerConfigOptions editorConfigOptions,
                                                                            OptionSet visualStudioOptions,
                                                                            OptionUpdater updater,
                                                                            string fileName,
                                                                            IEditorConfigData editorConfigData,
                                                                            string? description = null)
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<int>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            description ??= editorConfigData.GetSettingNameDocumentation();
            return new PerLanguageIntegerWhitespaceSetting(option, description, editorConfigOptions, visualStudioOptions, updater, location, editorConfigData);
        }

        public static PerLanguageWhitespaceSetting<bool> Create(PerLanguageOption2<bool> option,
                                                                            AnalyzerConfigOptions editorConfigOptions,
                                                                            OptionSet visualStudioOptions,
                                                                            OptionUpdater updater,
                                                                            string fileName,
                                                                            IEditorConfigData editorConfigData,
                                                                            string? description = null)
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<bool>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            description ??= editorConfigData.GetSettingNameDocumentation();
            return new PerLanguageBooleanWhitespaceSetting(option, description, editorConfigOptions, visualStudioOptions, updater, location, editorConfigData);
        }

        public static PerLanguageWhitespaceSetting<string> Create(PerLanguageOption2<string> option,
                                                                            AnalyzerConfigOptions editorConfigOptions,
                                                                            OptionSet visualStudioOptions,
                                                                            OptionUpdater updater,
                                                                            string fileName,
                                                                            IEditorConfigData editorConfigData,
                                                                            string? description = null)
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<string>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            description ??= editorConfigData.GetSettingNameDocumentation();
            return new PerLanguageStringWhitespaceSetting(option, description, editorConfigOptions, visualStudioOptions, updater, location, editorConfigData);
        }

        public static PerLanguageWhitespaceSetting<T> Create<T>(PerLanguageOption2<T> option,
                                                                            AnalyzerConfigOptions editorConfigOptions,
                                                                            OptionSet visualStudioOptions,
                                                                            OptionUpdater updater,
                                                                            string fileName,
                                                                            IEditorConfigData editorConfigData,
                                                                            string? description = null)
            where T : Enum
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<T>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            description ??= editorConfigData.GetSettingNameDocumentation();
            return new PerLanguageEnumWhitespaceSetting<T>(option, description, editorConfigOptions, visualStudioOptions, updater, location, editorConfigData);
        }

        public static WhitespaceSetting<int> Create(Option2<int> option,
                                                        AnalyzerConfigOptions editorConfigOptions,
                                                        OptionSet visualStudioOptions,
                                                        OptionUpdater updater,
                                                        string fileName,
                                                        IEditorConfigData editorConfigData,
                                                        string? description = null)
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<int>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            description ??= editorConfigData.GetSettingNameDocumentation();
            return new IntegerWhitespaceSetting(option, description, editorConfigOptions, visualStudioOptions, updater, location, editorConfigData);
        }

        public static WhitespaceSetting<bool> Create(Option2<bool> option,
                                                         AnalyzerConfigOptions editorConfigOptions,
                                                         OptionSet visualStudioOptions,
                                                         OptionUpdater updater,
                                                         string fileName,
                                                         IEditorConfigData editorConfigData,
                                                         string? description = null)
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<bool>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            description ??= editorConfigData.GetSettingNameDocumentation();
            return new BooleanWhitespaceSetting(option, description, editorConfigOptions, visualStudioOptions, updater, location, editorConfigData);
        }

        public static WhitespaceSetting<string> Create(Option2<string> option,
                                                           AnalyzerConfigOptions editorConfigOptions,
                                                           OptionSet visualStudioOptions,
                                                           OptionUpdater updater,
                                                           string fileName,
                                                           IEditorConfigData editorConfigData,
                                                           string? description = null)
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<string>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            description ??= editorConfigData.GetSettingNameDocumentation();
            return new StringWhitespaceSetting(option, description, editorConfigOptions, visualStudioOptions, updater, location, editorConfigData);
        }
        public static WhitespaceSetting<T> Create<T>(Option2<T> option,
                                                         AnalyzerConfigOptions editorConfigOptions,
                                                         OptionSet visualStudioOptions,
                                                         OptionUpdater updater,
                                                         string fileName,
                                                         IEditorConfigData editorConfigData,
                                                         string? description = null)
            where T : Enum
        {
            var isDefinedInEditorConfig = editorConfigOptions.TryGetEditorConfigOption<T>(option, out _);
            var location = new SettingLocation(isDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            description ??= editorConfigData.GetSettingNameDocumentation();
            return new EnumWhitespaceSetting<T>(option, description, editorConfigOptions, visualStudioOptions, updater, location, editorConfigData);
        }

        public string? GetSettingName()
        {
            return EditorConfigData.GetSettingName();
        }

        public string GetDocumentation()
        {
            return EditorConfigData.GetSettingNameDocumentation();
        }

        public ImmutableArray<string>? GetSettingValues()
        {
            return EditorConfigData.GetAllSettingValues();
        }

        public string? GetValueDocumentation(string value)
        {
            return EditorConfigData.GetSettingValueDocumentation(value);
        }

        public bool AllowsMultipleValues()
        {
            return EditorConfigData.GetAllowsMultipleValues();
        }
    }
}
