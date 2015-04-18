// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    /// <summary>
    /// Get diff edits between two nodes.
    /// </summary>
    internal interface ISyntaxDiffService : ILanguageService
    {
        IEnumerable<Edit<TreeNode>> Diff(SyntaxNode oldRoot, SyntaxNode newRoot);
    }
}
