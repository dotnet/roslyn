// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Options for customizing code fix test behaviors.
    /// </summary>
    [Flags]
    public enum CodeFixTestBehaviors
    {
        /// <summary>
        /// No special behaviors apply.
        /// </summary>
        None = 0,

        /// <summary>
        /// Skip the Fix All in Document check.
        /// </summary>
        SkipFixAllInDocumentCheck = 1 << 0,

        /// <summary>
        /// Skip the Fix All in Project check.
        /// </summary>
        SkipFixAllInProjectCheck = 1 << 1,

        /// <summary>
        /// Skip the Fix All in Solution check.
        /// </summary>
        SkipFixAllInSolutionCheck = 1 << 2,

        /// <summary>
        /// Skip all Fix All checks.
        /// </summary>
        SkipFixAllCheck = SkipFixAllInDocumentCheck | SkipFixAllInProjectCheck | SkipFixAllInSolutionCheck,

        /// <summary>
        /// One run one code fix iteration.
        /// </summary>
        FixOne = 1 << 3,

        /// <summary>
        /// Skip a verification check that a code fix is not attempting to provide a fix for a non-local analyzer
        /// diagnostic. Non-local diagnostics cannot be calculated on-demand within a span of a document, so they only
        /// trigger code fixes in cases where they were already computed prior to the code fix request. Since local
        /// diagnostics provide a better end user experience, the testing library helps analyzer authors default to this
        /// behavior.
        /// </summary>
        SkipLocalDiagnosticCheck = 1 << 4,
    }
}
