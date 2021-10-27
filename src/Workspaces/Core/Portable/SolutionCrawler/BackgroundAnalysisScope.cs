// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal enum BackgroundAnalysisScope
    {
        // NOTE: Do not change existing field ordering/values as this scope is saved as a user option,
        //       and that would break users who have saved a non-default scope for the option.

        /// <summary>
        /// Analyzers are executed only for currently active document.
        /// Compiler analyzer is treated specially and executed for all open documents.
        /// </summary>
        ActiveFile = 0,

        /// <summary>
        /// All analyzers, including compiler analyzer, are executed for all open documents.
        /// </summary>
        OpenFiles = 1,

        /// <summary>
        /// All analyzers, including compiler analyzer, are executed for all documents in the current solution.
        /// </summary>
        FullSolution = 2,

        /// <summary>
        /// Analyzers are disabled for all documents.
        /// Compiler analyzer is treated specially and executed for all open documents.
        /// </summary>
        None = 3,

        Minimal = None,
        Default = OpenFiles,
    }
}
