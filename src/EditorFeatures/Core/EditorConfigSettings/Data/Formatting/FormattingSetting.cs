// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract class FormattingSetting
    {
        protected OptionUpdater Updater { get; }
        protected string? Language { get; }

        protected FormattingSetting(string description, OptionUpdater updater, string? language = null)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Updater = updater;
            Language = language;
        }

        public string Description { get; }
        public abstract string Category { get; }
        public abstract Type Type { get; }
        public abstract OptionKey2 Key { get; }
        public abstract void SetValue(object value);
        public abstract object? GetValue();
        public abstract bool IsDefinedInEditorConfig { get; }

        public static PerLanguageFormattingSetting<TOption> Create<TOption>(PerLanguageOption2<TOption> option,
                                                                            string description,
                                                                            AnalyzerConfigOptions editorConfigOptions,
                                                                            OptionSet visualStudioOptions,
                                                                            OptionUpdater updater)
            where TOption : notnull
        {
            return new PerLanguageFormattingSetting<TOption>(option, description, editorConfigOptions, visualStudioOptions, updater);
        }

        public static FormattingSetting<TOption> Create<TOption>(Option2<TOption> option,
                                                                 string description,
                                                                 AnalyzerConfigOptions editorConfigOptions,
                                                                 OptionSet visualStudioOptions,
                                                                 OptionUpdater updater)
            where TOption : struct
        {
            return new FormattingSetting<TOption>(option, description, editorConfigOptions, visualStudioOptions, updater);
        }
    }
}
