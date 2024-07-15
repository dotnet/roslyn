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

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Whitespace.ViewModel;

internal partial class WhitespaceViewModel : SettingsViewModelBase<
    Setting,
    WhitespaceViewModel.SettingsSnapshotFactory,
    WhitespaceViewModel.SettingsEntriesSnapshot>
{
    public WhitespaceViewModel(ISettingsProvider<Setting> data,
                               IWpfTableControlProvider controlProvider,
                               ITableManagerProvider tableMangerProvider)
        : base(data, controlProvider, tableMangerProvider)
    { }

    public override string Identifier => "Whitespace";

    protected override SettingsSnapshotFactory CreateSnapshotFactory(ISettingsProvider<Setting> data)
        => new(data);

    protected override IEnumerable<ColumnState2> GetInitialColumnStates()
        => new[]
        {
            new ColumnState2(ColumnDefinitions.Whitespace.Category, isVisible: false, width: 0, groupingPriority: 1),
            new ColumnState2(ColumnDefinitions.Whitespace.Description, isVisible: true, width: 0),
            new ColumnState2(ColumnDefinitions.Whitespace.Value, isVisible: true, width: 0),
            new ColumnState2(ColumnDefinitions.Whitespace.Location, isVisible: true, width: 0)
        };

    protected override string[] GetFixedColumns()
        => new[]
        {
            ColumnDefinitions.Whitespace.Category,
            ColumnDefinitions.Whitespace.Description,
            ColumnDefinitions.Whitespace.Value,
            ColumnDefinitions.Whitespace.Location
        };
}
