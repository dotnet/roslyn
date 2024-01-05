// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal enum CompilerDiagnosticsScope
    {
        // NOTE: Do not change existing field ordering/values as this scope is saved as a user option,
        //       and that would break users who have saved a non-default scope for the option.

        /// <summary>
        /// Compiler warnings and errors are disabled for all documents.
        /// </summary>
        None = 0,

        /// <summary>
        /// Compiler warnings and errors are computed for visible documents
        /// and open documents which had errors/warnings in the prior solution snapshot.
        /// We want to analyze such non-visible, open documents to ensure that these
        /// prior reported errors/warnings get cleared out from the error list if they are
        /// no longer valid in the latest solution snapshot, hence ensuring error list has
        /// no stale entries.
        /// </summary>
        VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics = 1,

        /// <summary>
        /// Compiler warnings and errors are computed for all open documents.
        /// </summary>
        OpenFiles = 2,

        /// <summary>
        /// Compiler warnings and errors are computed for all documents in the current solution.
        /// </summary>
        FullSolution = 3,
    }
}
