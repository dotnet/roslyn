// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal enum ProjectAnalysisSummary
    {
        /// <summary>
        /// Project hasn't been changed.
        /// </summary>
        NoChanges,

        /// <summary>
        /// Project contains syntactic and/or semantic errors.
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
