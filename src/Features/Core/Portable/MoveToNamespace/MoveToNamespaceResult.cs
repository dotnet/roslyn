// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal class MoveToNamespaceResult
    {
        public static readonly MoveToNamespaceResult Failed = new();

        public bool Succeeded { get; }
        public Solution UpdatedSolution { get; }
        public Solution OriginalSolution { get; }
        public DocumentId UpdatedDocumentId { get; }
        public ImmutableDictionary<string, ISymbol> NewNameOriginalSymbolMapping { get; }
        public string NewName { get; }

        public MoveToNamespaceResult(
            Solution originalSolution,
            Solution updatedSolution,
            DocumentId updatedDocumentId,
            ImmutableDictionary<string, ISymbol> newNameOriginalSymbolMapping)
        {
            OriginalSolution = originalSolution;
            UpdatedSolution = updatedSolution;
            UpdatedDocumentId = updatedDocumentId;
            NewNameOriginalSymbolMapping = newNameOriginalSymbolMapping;
            Succeeded = true;
        }

        private MoveToNamespaceResult()
            => Succeeded = false;
    }
}
