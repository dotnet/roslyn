// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


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
