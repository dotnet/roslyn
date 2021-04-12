﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
