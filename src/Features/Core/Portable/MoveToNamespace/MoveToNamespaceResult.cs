// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal class MoveToNamespaceResult
    {
        public static readonly MoveToNamespaceResult Failed = new MoveToNamespaceResult();

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
        {
            Succeeded = false;
        }
    }
}
