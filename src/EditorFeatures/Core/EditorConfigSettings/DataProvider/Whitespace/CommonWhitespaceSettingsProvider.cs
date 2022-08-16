// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.EditorConfigSettings;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.Whitespace
{
    internal class CommonWhitespaceSettingsProvider : SettingsProviderBase<WhitespaceSetting, OptionUpdater, IOption2, object>
    {
        public CommonWhitespaceSettingsProvider(string fileName, OptionUpdater settingsUpdater, Workspace workspace)
            : base(fileName, settingsUpdater, workspace)
        {
            Update();
        }

        protected override void UpdateOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions)
        {
            var defaultOptions = GetDefaultOptions(editorConfigOptions, visualStudioOptions, SettingsUpdater);
            AddRange(defaultOptions);
        }

        private IEnumerable<WhitespaceSetting> GetDefaultOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions, OptionUpdater updater)
        {
            // Don't pass the editorfeatureresources string
            yield return WhitespaceSetting.Create(FormattingOptions2.UseTabs, editorConfigOptions, visualStudioOptions, updater, FileName, EditorConfigSettingsValueHolder.UseTabs);
            yield return WhitespaceSetting.Create(FormattingOptions2.TabSize, editorConfigOptions, visualStudioOptions, updater, FileName, EditorConfigSettingsValueHolder.TabSize);
            yield return WhitespaceSetting.Create(FormattingOptions2.IndentationSize, editorConfigOptions, visualStudioOptions, updater, FileName, EditorConfigSettingsValueHolder.IndentationSize);
            yield return WhitespaceSetting.Create(FormattingOptions2.NewLine, editorConfigOptions, visualStudioOptions, updater, FileName, EditorConfigSettingsValueHolder.NewLine);
            yield return WhitespaceSetting.Create(FormattingOptions2.InsertFinalNewLine, editorConfigOptions, visualStudioOptions, updater, FileName, EditorConfigSettingsValueHolder.InsertFinalNewLine);
            yield return WhitespaceSetting.Create(CodeStyleOptions2.OperatorPlacementWhenWrapping, editorConfigOptions, visualStudioOptions, updater, FileName, EditorConfigSettingsValueHolder.OperatorPlacementWhenWrapping);
        }
    }
}
