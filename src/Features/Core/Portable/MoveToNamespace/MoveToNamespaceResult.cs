// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal class MoveToNamespaceResult
    {
        public static readonly MoveToNamespaceResult Failed = new MoveToNamespaceResult();

        public bool Succeeded { get; }
        public Solution UpdatedSolution { get; }
        public DocumentId UpdatedDocumentId { get; }

        public MoveToNamespaceResult(Solution solution, DocumentId updatedDocumentId)
        {
            UpdatedSolution = solution;
            UpdatedDocumentId = updatedDocumentId;
            Succeeded = true;
        }

        private MoveToNamespaceResult()
        {
            Succeeded = false;
        }
    }
}
