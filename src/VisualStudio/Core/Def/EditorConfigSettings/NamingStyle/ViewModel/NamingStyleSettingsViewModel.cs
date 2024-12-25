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

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.ViewModel;

internal partial class NamingStyleSettingsViewModel : SettingsViewModelBase<
        NamingStyleSetting,
        NamingStyleSettingsViewModel.SettingsSnapshotFactory,
        NamingStyleSettingsViewModel.SettingsEntriesSnapshot>
{
    public NamingStyleSettingsViewModel(
        ISettingsProvider<NamingStyleSetting> data,
        IWpfTableControlProvider controlProvider,
        ITableManagerProvider tableMangerProvider)
    : base(data, controlProvider, tableMangerProvider) { }

    public override string Identifier => "NamingStyleSettings";

    protected override SettingsSnapshotFactory CreateSnapshotFactory(ISettingsProvider<NamingStyleSetting> data)
        => new(data);

    protected override IEnumerable<ColumnState2> GetInitialColumnStates()
        => new[]
        {
            new ColumnState2(ColumnDefinitions.NamingStyle.Type, isVisible: true, width: 0),
            new ColumnState2(ColumnDefinitions.NamingStyle.Style, isVisible: true, width: 0),
            new ColumnState2(ColumnDefinitions.NamingStyle.Severity, isVisible: true, width: 0),
            new ColumnState2(ColumnDefinitions.NamingStyle.Location, isVisible: true, width: 0)
        };

    protected override string[] GetFixedColumns()
        => [
            ColumnDefinitions.NamingStyle.Type,
            ColumnDefinitions.NamingStyle.Style,
            ColumnDefinitions.NamingStyle.Severity,
            ColumnDefinitions.NamingStyle.Location
        ];
}
