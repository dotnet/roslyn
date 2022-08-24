// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows.Input;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.EditorConfigSettings;
using Microsoft.CodeAnalysis.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract partial class CodeStyleSetting : IEditorConfigSettingInfo
    {
        public string Description { get; }

        protected readonly OptionUpdater Updater;

        public abstract string Category { get; }
        public abstract object? Value { get; }
        public abstract Type Type { get; }
        public abstract string[] GetValues();
        public abstract string GetCurrentValue();
        public abstract DiagnosticSeverity Severity { get; }
        public abstract bool IsDefinedInEditorConfig { get; }
        public abstract SettingLocation Location { get; protected set; }
        public abstract string? GetSettingName();
        public abstract string GetDocumentation();
        public abstract ImmutableArray<string>? GetSettingValues();
        public abstract bool SupportsSeverities();

        public CodeStyleSetting(string description, OptionUpdater updater)
        {
            Description = description;
            Updater = updater;
        }

        public void ChangeSeverity(DiagnosticSeverity severity)
        {
            var notification = severity switch
            {
                DiagnosticSeverity.Hidden => NotificationOption2.Silent,
                DiagnosticSeverity.Info => NotificationOption2.Suggestion,
                DiagnosticSeverity.Warning => NotificationOption2.Warning,
                DiagnosticSeverity.Error => NotificationOption2.Error,
                _ => NotificationOption2.None,
            };

            ChangeSeverity(notification);
        }

        protected abstract void ChangeSeverity(NotificationOption2 severity);
        public abstract void ChangeValue(int valueIndex);

        internal static CodeStyleSetting Create(Option2<CodeStyleOption2<bool>> option,
                                                AnalyzerConfigOptions editorConfigOptions,
                                                OptionSet visualStudioOptions,
                                                OptionUpdater updater,
                                                string fileName,
                                                IEditorConfigData editorConfigData,
                                                string? trueValueDescription = null,
                                                string? falseValueDescription = null,
                                                string description = "")
        {
            description = description == "" ? editorConfigData.GetSettingNameDocumentation() : description;
            return new BooleanCodeStyleSetting(option, description, trueValueDescription, falseValueDescription, editorConfigOptions, visualStudioOptions, updater, fileName, editorConfigData);
        }

        internal static CodeStyleSetting Create(PerLanguageOption2<CodeStyleOption2<bool>> option,
                                                AnalyzerConfigOptions editorConfigOptions,
                                                OptionSet visualStudioOptions,
                                                OptionUpdater updater,
                                                string fileName,
                                                IEditorConfigData editorConfigData,
                                                string? trueValueDescription = null,
                                                string? falseValueDescription = null,
                                                string description = "")
        {
            description = description == "" ? editorConfigData.GetSettingNameDocumentation() : description;
            return new PerLanguageBooleanCodeStyleSetting(option, description, trueValueDescription, falseValueDescription, editorConfigOptions, visualStudioOptions, updater, fileName, editorConfigData);
        }

        internal static CodeStyleSetting Create<T>(Option2<CodeStyleOption2<T>> option,
                                                   T[] enumValues,
                                                   string[] valueDescriptions,
                                                   AnalyzerConfigOptions editorConfigOptions,
                                                   OptionSet visualStudioOptions,
                                                   OptionUpdater updater,
                                                   string fileName,
                                                   IEditorConfigData editorConfigData,
                                                   string description = "")
            where T : Enum
        {
            description = description == "" ? editorConfigData.GetSettingNameDocumentation() : description;
            return new EnumCodeStyleSetting<T>(option, description, enumValues, valueDescriptions, editorConfigOptions, visualStudioOptions, updater, fileName, editorConfigData);
        }

        internal static CodeStyleSetting Create<T>(PerLanguageOption2<CodeStyleOption2<T>> option,
                                                   T[] enumValues,
                                                   string[] valueDescriptions,
                                                   AnalyzerConfigOptions editorConfigOptions,
                                                   OptionSet visualStudioOptions,
                                                   OptionUpdater updater,
                                                   string fileName,
                                                   IEditorConfigData editorConfigData,
                                                   string description = "")
            where T : Enum
        {
            description = description == "" ? editorConfigData.GetSettingNameDocumentation() : description;
            return new PerLanguageEnumCodeStyleSetting<T>(option, description, enumValues, valueDescriptions, editorConfigOptions, visualStudioOptions, updater, fileName, editorConfigData);
        }

        public bool AllowsMultipleValues() => false;
    }
}
