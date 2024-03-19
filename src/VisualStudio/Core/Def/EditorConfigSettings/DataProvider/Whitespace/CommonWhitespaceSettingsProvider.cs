// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.Whitespace;

internal sealed class CommonWhitespaceSettingsProvider : SettingsProviderBase<Setting, OptionUpdater, IOption2, object>
{
    public CommonWhitespaceSettingsProvider(string fileName, OptionUpdater settingsUpdater, Workspace workspace, IGlobalOptionService globalOptions)
        : base(fileName, settingsUpdater, workspace, globalOptions)
    {
        Update();
    }

    protected override void UpdateOptions(TieredAnalyzerConfigOptions options, ImmutableArray<Project> projectsInScope)
    {
        var defaultOptions = GetDefaultOptions(options, SettingsUpdater);
        AddRange(defaultOptions);
    }

    private static IEnumerable<Setting> GetDefaultOptions(TieredAnalyzerConfigOptions options, OptionUpdater updater)
    {
        yield return Setting.Create(FormattingOptions2.UseTabs, EditorFeaturesResources.Use_Tabs, options, updater);
        yield return Setting.Create(FormattingOptions2.TabSize, EditorFeaturesResources.Tab_Size, options, updater);
        yield return Setting.Create(FormattingOptions2.IndentationSize, EditorFeaturesResources.Indentation_Size, options, updater);
        yield return Setting.Create(FormattingOptions2.NewLine, EditorFeaturesResources.New_Line, options, updater);
        yield return Setting.Create(FormattingOptions2.InsertFinalNewLine, EditorFeaturesResources.Insert_Final_Newline, options, updater);
        yield return Setting.Create(CodeStyleOptions2.OperatorPlacementWhenWrapping, ServicesVSResources.Operator_placement_when_wrapping, options, updater);
    }
}
