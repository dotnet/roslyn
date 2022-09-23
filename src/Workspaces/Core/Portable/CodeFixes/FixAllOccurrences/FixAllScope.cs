// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Indicates scope for "Fix all occurrences" code fixes provided by each <see cref="FixAllProvider"/>.
    /// </summary>
    public enum FixAllScope
    {
        /// <summary>
        /// Scope to fix all occurences of diagnostic(s) in the entire document.
        /// </summary>
        Document,

        /// <summary>
        /// Scope to fix all occurences of diagnostic(s) in the entire project.
        /// </summary>
        Project,

        /// <summary>
        /// Scope to fix all occurences of diagnostic(s) in the entire solution.
        /// </summary>
        Solution,

        /// <summary>
        /// Custom scope to fix all occurences of diagnostic(s). This scope can
        /// be used by custom <see cref="FixAllProvider"/>s and custom code fix engines.
        /// </summary>
        Custom,

        /// <summary>
        /// Scope to fix all occurrences of diagnostic(s) in the containing member
        /// relative to the trigger span for the original code fix.
        /// </summary>
        ContainingMember,

        /// <summary>
        /// Scope to fix all occurrences of diagnostic(s) in the containing type
        /// relative to the trigger span for the original code fix.
        /// </summary>
        ContainingType,
    }
}
