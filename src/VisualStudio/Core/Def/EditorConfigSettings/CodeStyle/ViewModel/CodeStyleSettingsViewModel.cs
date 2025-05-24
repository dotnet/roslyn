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

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.ViewModel;

internal sealed partial class CodeStyleSettingsViewModel : SettingsViewModelBase<
    CodeStyleSetting,
    CodeStyleSettingsViewModel.SettingsSnapshotFactory,
    CodeStyleSettingsViewModel.SettingsEntriesSnapshot>
{
    public CodeStyleSettingsViewModel(ISettingsProvider<CodeStyleSetting> data,
                                      IWpfTableControlProvider controlProvider,
                                      ITableManagerProvider tableMangerProvider)
        : base(data, controlProvider, tableMangerProvider)
    { }

    public override string Identifier => "CodeStyleSettings";

    protected override SettingsSnapshotFactory CreateSnapshotFactory(ISettingsProvider<CodeStyleSetting> data)
        => new(data);

    protected override IEnumerable<ColumnState2> GetInitialColumnStates()
        => new[]
        {
            new ColumnState2(ColumnDefinitions.CodeStyle.Category, isVisible: false, width: 0, groupingPriority: 1),
            new ColumnState2(ColumnDefinitions.CodeStyle.Description, isVisible: true, width: 0),
            new ColumnState2(ColumnDefinitions.CodeStyle.Value, isVisible: true, width: 0),
            new ColumnState2(ColumnDefinitions.CodeStyle.Severity, isVisible: true, width: 0),
            new ColumnState2(ColumnDefinitions.CodeStyle.Location, isVisible: true, width: 0)
        };

    protected override string[] GetFixedColumns()
        => [
            ColumnDefinitions.CodeStyle.Category,
            ColumnDefinitions.CodeStyle.Description,
            ColumnDefinitions.CodeStyle.Value,
            ColumnDefinitions.CodeStyle.Severity,
            ColumnDefinitions.CodeStyle.Location
        ];
}
