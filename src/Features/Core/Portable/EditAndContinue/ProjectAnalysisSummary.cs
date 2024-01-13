// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal enum ProjectAnalysisSummary
    {
        /// <summary>
        /// Project hasn't been changed.
        /// </summary>
        NoChanges,

        /// <summary>
        /// Project contains compilation errors that block EnC analysis.
        /// </summary>
        CompilationErrors,

        /// <summary>
        /// Project contains rude edits.
        /// </summary>
        RudeEdits,

        /// <summary>
        /// The project only changed in comments, whitespaces, etc. that don't require compilation.
        /// </summary>
        ValidInsignificantChanges,

        /// <summary>
        /// The project contains valid changes that require application of a delta.
        /// </summary>
        ValidChanges
    }
}
