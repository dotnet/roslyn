// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// Contains changes that can be either applied to different targets such as a buffer or a tree
    /// or examined to be used in other places such as quick fix.
    /// </summary>
    internal interface IFormattingResult
    {
        IList<TextChange> GetTextChanges(CancellationToken cancellationToken = default(CancellationToken));
        SyntaxNode GetFormattedRoot(CancellationToken cancellationToken = default(CancellationToken));
    }
}
