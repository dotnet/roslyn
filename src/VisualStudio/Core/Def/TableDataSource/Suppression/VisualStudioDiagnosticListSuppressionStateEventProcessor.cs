// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;

[Export(typeof(ITableControlEventProcessorProvider))]
[DataSourceType(StandardTableDataSources.ErrorTableDataSource)]
[DataSource(StandardTableDataSources.AnyDataSource)]
[Name(nameof(VisualStudioDiagnosticListSuppressionStateEventProcessorProvider))]
[Order(After = Priority.Default)]
internal sealed class VisualStudioDiagnosticListSuppressionStateEventProcessorProvider : ITableControlEventProcessorProvider
{
    private readonly IVisualStudioDiagnosticListSuppressionStateService _suppressionStateService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioDiagnosticListSuppressionStateEventProcessorProvider(
        IVisualStudioDiagnosticListSuppressionStateService suppressionStateService)
    {
        _suppressionStateService = suppressionStateService;
    }

    public ITableControlEventProcessor GetAssociatedEventProcessor(IWpfTableControl tableControl)
        => new VisualStudioDiagnosticListSuppressionStateEventProcessor(tableControl, _suppressionStateService);
}

internal sealed class VisualStudioDiagnosticListSuppressionStateEventProcessor : TableControlEventProcessorBase
{
    private readonly IWpfTableControl _tableControl;
    private readonly IVisualStudioDiagnosticListSuppressionStateService _suppressionStateService;

    public VisualStudioDiagnosticListSuppressionStateEventProcessor(
        IWpfTableControl tableControl,
        IVisualStudioDiagnosticListSuppressionStateService suppressionStateService)
    {
        _tableControl = tableControl;
        _suppressionStateService = suppressionStateService;
        _tableControl.EntriesChanged += OnEntriesChanged;
        RefreshSelection();
    }

    public override void PostprocessSelectionChanged(TableSelectionChangedEventArgs e)
        => _suppressionStateService.ProcessSelectionChanged(e);

    private void OnEntriesChanged(object sender, EntriesChangedEventArgs e)
        => RefreshSelection();

    private void RefreshSelection()
        => _suppressionStateService.RefreshSelection(_tableControl.SelectedEntries);
}
