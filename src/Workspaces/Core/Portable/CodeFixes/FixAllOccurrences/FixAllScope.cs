// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Indicates scope for "Fix all occurrences" code fixes provided by each <see cref="FixAllProvider"/>.
    /// </summary>
    public enum FixAllScope
    {
        Document,
        Project,
        Solution,
        Custom
    }
}
