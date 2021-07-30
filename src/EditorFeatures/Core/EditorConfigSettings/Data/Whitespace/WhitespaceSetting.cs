// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract class WhitespaceSetting
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
    }
}
