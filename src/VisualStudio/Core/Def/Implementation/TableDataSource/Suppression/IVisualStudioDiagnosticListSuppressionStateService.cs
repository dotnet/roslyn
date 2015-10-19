// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Service to maintain information about the suppression state of specific set of items in the error list.
    /// </summary>
    /// <remarks>TODO: Move to the core platform layer.</remarks>
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
    }
}
