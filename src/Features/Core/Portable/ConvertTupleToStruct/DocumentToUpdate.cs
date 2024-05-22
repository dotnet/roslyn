// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ConvertTupleToStruct;

internal readonly struct DocumentToUpdate(Document document, ImmutableArray<SyntaxNode> nodesToUpdate)
{
    /// <summary>
    /// The document to update.
    /// </summary>
    public readonly Document Document = document;

    /// <summary>
    /// The subnodes in this document to walk and update.  If empty, the entire document
    /// should be walked.
    /// </summary>
    public readonly ImmutableArray<SyntaxNode> NodesToUpdate = nodesToUpdate;
}
