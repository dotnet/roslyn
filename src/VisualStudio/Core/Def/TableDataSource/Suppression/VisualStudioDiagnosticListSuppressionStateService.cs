// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;

/// <summary>
/// Service to maintain information about the suppression state of specific set of items in the error list.
/// </summary>
[Export(typeof(IVisualStudioDiagnosticListSuppressionStateService))]
[Export(typeof(VisualStudioDiagnosticListSuppressionStateService))]
internal sealed class VisualStudioDiagnosticListSuppressionStateService : IVisualStudioDiagnosticListSuppressionStateService
{
    private readonly IThreadingContext _threadingContext;
    private readonly VisualStudioWorkspace _workspace;

    private IVsUIShell? _shellService;
    private IWpfTableControl? _tableControl;

    private int _selectedActiveItems;
    private int _selectedSuppressedItems;
    private int _selectedRoslynItems;
    private int _selectedCompilerDiagnosticItems;
    private int _selectedNoLocationDiagnosticItems;
    private int _selectedNonSuppressionStateItems;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioDiagnosticListSuppressionStateService(
        IThreadingContext threadingContext,
        VisualStudioWorkspace workspace)
    {
        _threadingContext = threadingContext;
        _workspace = workspace;
    }

    public async Task InitializeAsync(IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        _shellService = await serviceProvider.GetServiceAsync<SVsUIShell, IVsUIShell>(throwOnFailure: false, cancellationToken).ConfigureAwait(false);
        var errorList = await serviceProvider.GetServiceAsync<SVsErrorList, IErrorList>(throwOnFailure: false, cancellationToken).ConfigureAwait(false);
        _tableControl = errorList?.TableControl;

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        ClearState();
        InitializeFromTableControlIfNeeded();
    }

    private int SelectedItems => _selectedActiveItems + _selectedSuppressedItems + _selectedNonSuppressionStateItems;

    // If we can suppress either in source or in suppression file, we enable suppress context menu.
    public bool CanSuppressSelectedEntries => CanSuppressSelectedEntriesInSource || CanSuppressSelectedEntriesInSuppressionFiles;

    // If at least one suppressed item is selected, we enable remove suppressions.
    public bool CanRemoveSuppressionsSelectedEntries => _selectedSuppressedItems > 0;

    // If at least one Roslyn active item with location is selected, we enable suppress in source.
    // Note that we do not support suppress in source when mix of Roslyn and non-Roslyn items are selected as in-source suppression has different meaning and implementation for these.
    public bool CanSuppressSelectedEntriesInSource => _selectedActiveItems > 0 &&
        _selectedRoslynItems == _selectedActiveItems &&
        (_selectedRoslynItems - _selectedNoLocationDiagnosticItems) > 0;

    // If at least one Roslyn active item is selected, we enable suppress in suppression file.
    // Also, compiler diagnostics cannot be suppressed in suppression file, so there must be at least one non-compiler item.
    public bool CanSuppressSelectedEntriesInSuppressionFiles => _selectedActiveItems > 0 &&
        (_selectedRoslynItems - _selectedCompilerDiagnosticItems) > 0;

    private void ClearState()
    {
        _selectedActiveItems = 0;
        _selectedSuppressedItems = 0;
        _selectedRoslynItems = 0;
        _selectedCompilerDiagnosticItems = 0;
        _selectedNoLocationDiagnosticItems = 0;
        _selectedNonSuppressionStateItems = 0;
    }

    private void InitializeFromTableControlIfNeeded()
    {
        if (_tableControl == null)
        {
            return;
        }

        if (SelectedItems == _tableControl.SelectedEntries.Count())
        {
            // We already have up-to-date state data, so don't need to re-compute.
            return;
        }

        ClearState();
        if (ProcessEntries(_tableControl.SelectedEntries, added: true))
        {
            UpdateQueryStatus();
        }
    }

    /// <summary>
    /// Updates suppression state information when the selected entries change in the error list.
    /// </summary>
    public void ProcessSelectionChanged(TableSelectionChangedEventArgs e)
    {
        var hasAddedSuppressionStateEntry = ProcessEntries(e.AddedEntries, added: true);
        var hasRemovedSuppressionStateEntry = ProcessEntries(e.RemovedEntries, added: false);

        // If any entry that supports suppression state was ever involved, update query status since each item in the error list
        // can have different context menu.
        if (hasAddedSuppressionStateEntry || hasRemovedSuppressionStateEntry)
        {
            UpdateQueryStatus();
        }

        InitializeFromTableControlIfNeeded();
    }

    private bool ProcessEntries(IEnumerable<ITableEntryHandle> entryHandles, bool added)
    {
        var hasSuppressionStateEntry = false;
        foreach (var entryHandle in entryHandles)
        {
            if (EntrySupportsSuppressionState(entryHandle, out var isRoslynEntry, out var isSuppressedEntry, out var isCompilerDiagnosticEntry, out var isNoLocationDiagnosticEntry))
            {
                hasSuppressionStateEntry = true;
                HandleSuppressionStateEntry(isRoslynEntry, isSuppressedEntry, isCompilerDiagnosticEntry, isNoLocationDiagnosticEntry, added);
            }
            else
            {
                HandleNonSuppressionStateEntry(added);
            }
        }

        return hasSuppressionStateEntry;
    }

    private static bool EntrySupportsSuppressionState(ITableEntryHandle entryHandle, out bool isRoslynEntry, out bool isSuppressedEntry, out bool isCompilerDiagnosticEntry, out bool isNoLocationDiagnosticEntry)
    {
        isNoLocationDiagnosticEntry = !entryHandle.TryGetValue(StandardTableColumnDefinitions.DocumentName, out string filePath) ||
            string.IsNullOrEmpty(filePath);

        isRoslynEntry = false;
        isCompilerDiagnosticEntry = false;
        return IsNonRoslynEntrySupportingSuppressionState(entryHandle, out isSuppressedEntry);
    }

    private static bool IsNonRoslynEntrySupportingSuppressionState(ITableEntryHandle entryHandle, out bool isSuppressedEntry)
    {
        if (entryHandle.TryGetValue(StandardTableKeyNames.SuppressionState, out SuppressionState suppressionStateValue))
        {
            isSuppressedEntry = suppressionStateValue == SuppressionState.Suppressed;
            return true;
        }

        isSuppressedEntry = false;
        return false;
    }

    /// <summary>
    /// Returns true if an entry's suppression state can be modified.
    /// </summary>
    private static bool IsEntryWithConfigurableSuppressionState([NotNullWhen(true)] DiagnosticData? entry)
        => entry != null && !SuppressionHelpers.IsNotConfigurableDiagnostic(entry);

    /// <summary>
    /// Gets <see cref="DiagnosticData"/> objects for selected error list entries.
    /// For remove suppression, the method also returns selected external source diagnostics.
    /// </summary>
    public async Task<ImmutableArray<DiagnosticData>> GetSelectedItemsAsync(bool isAddSuppression, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_tableControl);

        using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var builder);

        Dictionary<string, Project>? projectNameToProjectMap = null;
        Dictionary<Project, ImmutableDictionary<string, Document>>? filePathToDocumentMap = null;

        foreach (var entryHandle in _tableControl.SelectedEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DiagnosticData? diagnosticData = null;

            if (!isAddSuppression)
            {
                // For suppression removal, we also need to handle FxCop entries.
                if (!IsNonRoslynEntrySupportingSuppressionState(entryHandle, out var isSuppressedEntry) ||
                    !isSuppressedEntry)
                {
                    continue;
                }

                string? filePath = null;
                var line = -1; // FxCop only supports line, not column.

                if (entryHandle.TryGetValue(StandardTableColumnDefinitions.ErrorCode, out string errorCode) && !string.IsNullOrEmpty(errorCode) &&
                    entryHandle.TryGetValue(StandardTableColumnDefinitions.ErrorCategory, out string category) && !string.IsNullOrEmpty(category) &&
                    entryHandle.TryGetValue(StandardTableColumnDefinitions.Text, out string message) && !string.IsNullOrEmpty(message) &&
                    entryHandle.TryGetValue(StandardTableColumnDefinitions.ProjectName, out string projectName) && !string.IsNullOrEmpty(projectName))
                {
                    if (projectNameToProjectMap == null)
                    {
                        projectNameToProjectMap = [];
                        foreach (var p in _workspace.CurrentSolution.Projects)
                        {
                            projectNameToProjectMap[p.Name] = p;
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (!projectNameToProjectMap.TryGetValue(projectName, out var project))
                    {
                        // bail out
                        continue;
                    }

                    Document? document = null;
                    var hasLocation =
                        entryHandle.TryGetValue(StandardTableColumnDefinitions.DocumentName, out filePath) && !string.IsNullOrEmpty(filePath) &&
                        entryHandle.TryGetValue(StandardTableColumnDefinitions.Line, out line) && line >= 0;
                    if (!hasLocation)
                        continue;

                    if (RoslynString.IsNullOrEmpty(filePath) || line < 0)
                    {
                        // bail out
                        continue;
                    }

                    filePathToDocumentMap ??= [];
                    if (!filePathToDocumentMap.TryGetValue(project, out var filePathMap))
                    {
                        filePathMap = await GetFilePathToDocumentMapAsync(project, cancellationToken).ConfigureAwait(false);
                        filePathToDocumentMap[project] = filePathMap;
                    }

                    if (!filePathMap.TryGetValue(filePath, out document))
                    {
                        // bail out
                        continue;
                    }

                    // TODO: should we use the tree of the document (if available) to get the correct mapped span for this location?
                    var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                    var linePosition = new LinePosition(line, 0);
                    var linePositionSpan = new LinePositionSpan(start: linePosition, end: linePosition);
                    var location = new DiagnosticDataLocation(
                        new FileLinePositionSpan(filePath, linePositionSpan), document.Id);

                    Contract.ThrowIfNull(project);

                    // Create a diagnostic with correct values for fields we care about: id, category, message, isSuppressed, location
                    // and default values for the rest of the fields (not used by suppression fixer).
                    diagnosticData = new DiagnosticData(
                        id: errorCode,
                        category: category,
                        message: message,
                        severity: DiagnosticSeverity.Warning,
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        warningLevel: 1,
                        isSuppressed: isSuppressedEntry,
                        title: message,
                        location: location,
                        customTags: SuppressionHelpers.SynthesizedExternalSourceDiagnosticCustomTags,
                        properties: ImmutableDictionary<string, string?>.Empty,
                        projectId: project.Id);
                }
            }

            if (IsEntryWithConfigurableSuppressionState(diagnosticData))
            {
                builder.Add(diagnosticData);
            }
        }

        return builder.ToImmutableAndClear();
    }

    private static async Task<ImmutableDictionary<string, Document>> GetFilePathToDocumentMapAsync(Project project, CancellationToken cancellationToken)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, Document>();
        foreach (var document in project.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var filePath = tree.FilePath;
            if (filePath != null)
            {
                builder.Add(filePath, document);
            }
        }

        return builder.ToImmutable();
    }

    private static void UpdateSelectedItems(bool added, ref int count)
    {
        if (added)
        {
            count++;
        }
        else
        {
            count--;
        }
    }

    private void HandleSuppressionStateEntry(bool isRoslynEntry, bool isSuppressedEntry, bool isCompilerDiagnosticEntry, bool isNoLocationDiagnosticEntry, bool added)
    {
        if (isRoslynEntry)
        {
            UpdateSelectedItems(added, ref _selectedRoslynItems);
        }

        if (isCompilerDiagnosticEntry)
        {
            UpdateSelectedItems(added, ref _selectedCompilerDiagnosticItems);
        }

        if (isNoLocationDiagnosticEntry)
        {
            UpdateSelectedItems(added, ref _selectedNoLocationDiagnosticItems);
        }

        if (isSuppressedEntry)
        {
            UpdateSelectedItems(added, ref _selectedSuppressedItems);
        }
        else
        {
            UpdateSelectedItems(added, ref _selectedActiveItems);
        }
    }

    private void HandleNonSuppressionStateEntry(bool added)
        => UpdateSelectedItems(added, ref _selectedNonSuppressionStateItems);

    private void UpdateQueryStatus()
    {
        // Force the shell to refresh the QueryStatus for all the command since default behavior is it only does query
        // when focus on error list has changed, not individual items.
        _shellService?.UpdateCommandUI(0);
    }
}
