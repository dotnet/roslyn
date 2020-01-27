// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ActiveStatementId
    {
        public readonly DocumentId DocumentId;
        public readonly int Ordinal;

        public ActiveStatementId(DocumentId documentId, int ordinal)
        {
            DocumentId = documentId;
            Ordinal = ordinal;
        }
    }
}
