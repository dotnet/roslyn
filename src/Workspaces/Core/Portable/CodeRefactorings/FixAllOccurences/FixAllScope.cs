// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Indicates scope for "Fix all occurrences" for code refactorings provided by each <see cref="CodeRefactoringProvider"/>.
    /// </summary>
    public enum FixAllScope
    {
        Document,
        Project,
        Solution,
        Selection,
        ContainingMember,
        ContainingType,

        Custom = int.MaxValue
    }
}
