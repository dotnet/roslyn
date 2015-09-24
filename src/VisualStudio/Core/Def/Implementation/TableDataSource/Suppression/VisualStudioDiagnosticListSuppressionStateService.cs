// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Service to maintain information about the suppression state of specific set of items in the error list.
    /// </summary>
    [Export(typeof(IVisualStudioDiagnosticListSuppressionStateService))]
    internal class VisualStudioDiagnosticListSuppressionStateService : IVisualStudioDiagnosticListSuppressionStateService
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly IVsUIShell _shellService;
        private readonly IWpfTableControl _tableControl;

        private int _selectedActiveItems;
        private int _selectedSuppressedItems;
        private int _selectedRoslynItems;
        private int _selectedCompilerDiagnosticItems;
        private int _selectedNonSuppressionStateItems;

        [ImportingConstructor]
        public VisualStudioDiagnosticListSuppressionStateService(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
            _shellService = (IVsUIShell)serviceProvider.GetService(typeof(SVsUIShell));
            var errorList = serviceProvider.GetService(typeof(SVsErrorList)) as IErrorList;
            _tableControl = errorList?.TableControl;

            ClearState();
            InitializeFromTableControlIfNeeded();
        }

        private int SelectedItems => _selectedActiveItems + _selectedSuppressedItems + _selectedNonSuppressionStateItems;

        // If we can suppress either in source or in suppression file, we enable suppress context menu.
        public bool CanSuppressSelectedEntries => CanSuppressSelectedEntriesInSource || CanSuppressSelectedEntriesInSuppressionFiles;

        // If only suppressed items are selected, we enable remove suppressions.
        public bool CanRemoveSuppressionsSelectedEntries => _selectedActiveItems == 0 && _selectedSuppressedItems > 0;

        // If only Roslyn active items are selected, we enable suppress in source.
        public bool CanSuppressSelectedEntriesInSource => _selectedActiveItems > 0 &&
            _selectedSuppressedItems == 0 &&
            _selectedRoslynItems == _selectedActiveItems;

        // If only active items are selected, and there is at least one Roslyn item, we enable suppress in suppression file.
        // Also, compiler diagnostics cannot be suppressed in suppression file, so there must be at least one non-compiler item.
        public bool CanSuppressSelectedEntriesInSuppressionFiles => _selectedActiveItems > 0 &&
            _selectedSuppressedItems == 0 &&
            (_selectedRoslynItems - _selectedCompilerDiagnosticItems) > 0;

        private void ClearState()
        {
            _selectedActiveItems = 0;
            _selectedSuppressedItems = 0;
            _selectedRoslynItems = 0;
            _selectedCompilerDiagnosticItems = 0;
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
            bool isRoslynEntry, isSuppressedEntry, isCompilerDiagnosticEntry;
            var hasSuppressionStateEntry = false;
            foreach (var entryHandle in entryHandles)
            {
                if (EntrySupportsSuppressionState(entryHandle, out isRoslynEntry, out isSuppressedEntry, out isCompilerDiagnosticEntry))
                {
                    hasSuppressionStateEntry = true;
                    HandleSuppressionStateEntry(isRoslynEntry, isSuppressedEntry, isCompilerDiagnosticEntry, added);
                }
                else
                {
                    HandleNonSuppressionStateEntry(added);
                }
            }

            return hasSuppressionStateEntry;
        }

        private static bool EntrySupportsSuppressionState(ITableEntryHandle entryHandle, out bool isRoslynEntry, out bool isSuppressedEntry, out bool isCompilerDiagnosticEntry)
        {
            int index;
            var roslynSnapshot = GetEntriesSnapshot(entryHandle, out index);

            var diagnosticData = roslynSnapshot?.GetItem(index)?.Primary;
            if (diagnosticData == null || !diagnosticData.HasTextSpan || SuppressionHelpers.IsNotConfigurableDiagnostic(diagnosticData))
            {
                isRoslynEntry = false;
                isSuppressedEntry = false;
                isCompilerDiagnosticEntry = false;
                return false;
            }

            isRoslynEntry = true;
            isSuppressedEntry = diagnosticData.IsSuppressed;
            isCompilerDiagnosticEntry = SuppressionHelpers.IsCompilerDiagnostic(diagnosticData);
            return true;
        }

        private static AbstractTableEntriesSnapshot<DiagnosticData> GetEntriesSnapshot(ITableEntryHandle entryHandle)
        {
            int index;
            return GetEntriesSnapshot(entryHandle, out index);
        }

        private static AbstractTableEntriesSnapshot<DiagnosticData> GetEntriesSnapshot(ITableEntryHandle entryHandle, out int index)
        {
            ITableEntriesSnapshot snapshot;
            if (!entryHandle.TryGetSnapshot(out snapshot, out index))
            {
                return null;
            }

            return snapshot as AbstractTableEntriesSnapshot<DiagnosticData>;
        }

        /// <summary>
        /// Gets <see cref="DiagnosticData"/> objects for error list entries, filtered based on the given parameters.
        /// </summary>
        public ImmutableArray<DiagnosticData> GetItems(bool selectedEntriesOnly, bool isAddSuppression, bool isSuppressionInSource, bool onlyCompilerDiagnostics, CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<DiagnosticData>();
            var entries = selectedEntriesOnly ? _tableControl.SelectedEntries : _tableControl.Entries;
            foreach (var entryHandle in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                DiagnosticData diagnosticData = null;
                int index;
                var roslynSnapshot = GetEntriesSnapshot(entryHandle, out index);
                if (roslynSnapshot != null)
                {
                    diagnosticData = roslynSnapshot.GetItem(index)?.Primary;
                    if (diagnosticData != null && diagnosticData.HasTextSpan)
                    {
                        var isCompilerDiagnostic = SuppressionHelpers.IsCompilerDiagnostic(diagnosticData);
                        if (onlyCompilerDiagnostics && !isCompilerDiagnostic)
                        {
                            continue;
                        }

                        if (isAddSuppression)
                        {
                            // Compiler diagnostics can only be suppressed in source.
                            if (!diagnosticData.IsSuppressed &&
                                (isSuppressionInSource || !isCompilerDiagnostic))
                            {
                                builder.Add(diagnosticData);
                            }
                        }
                        else if (diagnosticData.IsSuppressed)
                        {
                            builder.Add(diagnosticData);
                        }
                    }
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

        private void HandleSuppressionStateEntry(bool isRoslynEntry, bool isSuppressedEntry, bool isCompilerDiagnosticEntry, bool added)
        {
            if (isRoslynEntry)
            {
                UpdateSelectedItems(added, ref _selectedRoslynItems);
            }

            if (isCompilerDiagnosticEntry)
            {
                UpdateSelectedItems(added, ref _selectedCompilerDiagnosticItems);
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
        {
            UpdateSelectedItems(added, ref _selectedNonSuppressionStateItems);
        }

        private void UpdateQueryStatus()
        {
            // Force the shell to refresh the QueryStatus for all the command since default behavior is it only does query
            // when focus on error list has changed, not individual items.
            if (_shellService != null)
            {
                _shellService.UpdateCommandUI(0);
            }
        }
    }
}
