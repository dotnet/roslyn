// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.ViewModel;

internal partial class AnalyzerSettingsViewModel : SettingsViewModelBase<
    AnalyzerSetting,
    AnalyzerSettingsViewModel.SettingsSnapshotFactory,
    AnalyzerSettingsViewModel.SettingsEntriesSnapshot>
{

    public AnalyzerSettingsViewModel(ISettingsProvider<AnalyzerSetting> data,
                                     IWpfTableControlProvider controlProvider,
                                     ITableManagerProvider tableMangerProvider)
        : base(data, controlProvider, tableMangerProvider)
    { }

    public override string Identifier => "AnalyzerSettings";

    protected override SettingsSnapshotFactory CreateSnapshotFactory(ISettingsProvider<AnalyzerSetting> data)
        => new(data);

    protected override IEnumerable<ColumnState2> GetInitialColumnStates()
        => new[]
        {
            new ColumnState2(ColumnDefinitions.Analyzer.Id, isVisible: true, width: 0),
            new ColumnState2(ColumnDefinitions.Analyzer.Title, isVisible: true, width: 0),
            new ColumnState2(ColumnDefinitions.Analyzer.Description, isVisible: false, width: 0),
            new ColumnState2(ColumnDefinitions.Analyzer.Category, isVisible: true, width: 0, groupingPriority: 1),
            new ColumnState2(ColumnDefinitions.Analyzer.Severity, isVisible: true, width: 0),
            new ColumnState2(ColumnDefinitions.Analyzer.Location, isVisible: true, width: 0)
        };

    protected override string[] GetFixedColumns()
        => new[]
        {
            ColumnDefinitions.Analyzer.Category,
            ColumnDefinitions.Analyzer.Id,
            ColumnDefinitions.Analyzer.Title,
            ColumnDefinitions.Analyzer.Description,
            ColumnDefinitions.Analyzer.Severity,
            ColumnDefinitions.Analyzer.Location,
        };
}
