// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data
{
    internal abstract partial class CodeStyleSetting
    {
        private class PerLanguageEnumCodeStyleSetting<T> : EnumCodeStyleSettingBase<T>
            where T : Enum
        {
            private readonly PerLanguageOption2<CodeStyleOption2<T>> _option;
            private readonly AnalyzerConfigOptions _editorConfigOptions;
            private readonly OptionSet _visualStudioOptions;

            public PerLanguageEnumCodeStyleSetting(PerLanguageOption2<CodeStyleOption2<T>> option,
                                                   string description,
                                                   T[] enumValues,
                                                   string[] valueDescriptions,
                                                   AnalyzerConfigOptions editorConfigOptions,
                                                   OptionSet visualStudioOptions,
                                                   OptionUpdater updater,
                                                   string fileName)
                : base(description, enumValues, valueDescriptions, option.Group.Description, updater)
            {
                _option = option;

                _editorConfigOptions = editorConfigOptions;
                _visualStudioOptions = visualStudioOptions;
                Location = new SettingLocation(IsDefinedInEditorConfig ? LocationKind.EditorConfig : LocationKind.VisualStudio, fileName);
            }

            public override bool IsDefinedInEditorConfig => _editorConfigOptions.TryGetEditorConfigOption<CodeStyleOption2<T>>(_option, out _);

            public override SettingLocation Location { get; protected set; }

            protected override void ChangeSeverity(NotificationOption2 severity)
            {
                ICodeStyleOption option = GetOption();
                Location = Location with { LocationKind = LocationKind.EditorConfig };
                Updater.QueueUpdate(_option, option.WithNotification(severity));
            }

            public override void ChangeValue(int valueIndex)
            {
                ICodeStyleOption option = GetOption();
                Location = Location with { LocationKind = LocationKind.EditorConfig };
                Updater.QueueUpdate(_option, option.WithValue(_enumValues[valueIndex]));
            }

            protected override CodeStyleOption2<T> GetOption()
                => _editorConfigOptions.TryGetEditorConfigOption(_option, out CodeStyleOption2<T>? value) && value is not null
                    ? value
                    // TODO(jmarolf): Should we expose duplicate options if the user has a different setting in VB vs. C#?
                    //                Today this code will choose whatever option is set for C# as the default.
                    : _visualStudioOptions.GetOption<CodeStyleOption2<T>>(new OptionKey2(_option, LanguageNames.CSharp));

            public override string? GetSettingName()
            {
                var storageLocation = GetEditorConfigStorageLocation(_option);
                return storageLocation?.KeyName;
            }

            public override string GetDocumentation()
            {
                return Description;
            }

            public override ImmutableArray<string>? GetSettingValues(OptionSet optionSet)
            {
                var type = typeof(T);
                var strings = new List<string>();
                var enumValues = type.GetEnumValues();

                foreach (var enumValue in enumValues)
                {
                    var storageLocation = GetEditorConfigStorageLocation(_option);
                    var codeStyleSetting = new CodeStyleOption2<T>((T)enumValue!, NotificationOption2.Silent);
                    var option = storageLocation?.GetEditorConfigStringValue(codeStyleSetting, optionSet);
                    if (option != null)
                    {
                        option = option.Contains(':') ? option.Split(':').First() : option;
                        strings.Add(option);
                    }
                }

                return strings.ToImmutableArray();
            }
        }
    }
}
