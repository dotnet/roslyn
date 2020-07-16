// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
