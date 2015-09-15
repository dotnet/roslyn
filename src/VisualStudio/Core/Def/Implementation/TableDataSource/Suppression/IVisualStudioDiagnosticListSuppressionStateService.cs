// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Service to maintain information about the suppression state of specific set of items in the error list.
    /// </summary>
    internal interface IVisualStudioDiagnosticListSuppressionStateService
    {
        /// <summary>
        /// Indicates if the top level "Suppress" menu should be visible for the current error list selection.
        /// </summary>
        bool CanSuppressSelectedEntries { get; }

        /// <summary>
        /// Indicates if sub-menu "(Suppress) In Source" menu should be visible for the current error list selection.
        /// </summary>
        bool CanSuppressSelectedEntriesInSource { get; }

        /// <summary>
        /// Indicates if sub-menu "(Suppress) In Suppression File" menu should be visible for the current error list selection.
        /// </summary>
        bool CanSuppressSelectedEntriesInSuppressionFiles { get; }

        /// <summary>
        /// Indicates if the top level "Remove Suppression(s)" menu should be visible for the current error list selection.
        /// </summary>
        bool CanRemoveSuppressionsSelectedEntries { get; }

        /// <summary>
        /// Updates suppression state information when the selected entries change in the error list.
        /// </summary>
        /// <param name="e"></param>
        void ProcessSelectionChanged(TableSelectionChangedEventArgs e);

        /// <summary>
        /// Gets <see cref="DiagnosticData"/> objects for error list entries, filtered based on the given parameters.
        /// </summary>
        ImmutableArray<DiagnosticData> GetItems(bool selectedEntriesOnly, bool isAddSuppression, bool isSuppressionInSource, CancellationToken cancellationToken);
    }
}
