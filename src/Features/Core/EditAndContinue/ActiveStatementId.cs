// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal struct ActiveStatementId
    {
        public readonly DocumentId DocumentId;
        public readonly int Ordinal;

        public ActiveStatementId(DocumentId documentId, int ordinal)
        {
            this.DocumentId = documentId;
            this.Ordinal = ordinal;
        }
    }
}
