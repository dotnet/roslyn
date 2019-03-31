// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ConvertTupleToStruct
{
    internal readonly struct DocumentToUpdate
    {
        /// <summary>
        /// The document to update.
        /// </summary>
        public readonly Document Document;

        /// <summary>
        /// The subnodes in this document to walk and update.  If empty, the entire document
        /// should be walked.
        /// </summary>
        public readonly ImmutableArray<SyntaxNode> NodesToUpdate;

        public DocumentToUpdate(Document document, ImmutableArray<SyntaxNode> nodesToUpdate)
        {
            Document = document;
            NodesToUpdate = nodesToUpdate;
        }
    }
}
