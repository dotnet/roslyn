// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal class SyncNamespaceDocumentsNotInSolutionException(ImmutableArray<DocumentId> documentIds) : Exception
    {
        private readonly ImmutableArray<DocumentId> _documentIds = documentIds;

        public override string ToString()
        {
            using var _ = PooledStringBuilder.GetInstance(out var builder);
            foreach (var documentId in _documentIds)
            {
                builder.AppendLine($"{documentId.GetDebuggerDisplay()}, IsSourceGeneratedDocument: {documentId.IsSourceGenerated}");
            }

            return builder.ToString();
        }
    }
}
