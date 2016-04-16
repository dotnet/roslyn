// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal sealed class ExtractInterfaceResult
    {
        public bool Succeeded { get; }
        public Solution UpdatedSolution { get; }
        public DocumentId NavigationDocumentId { get; }

        public ExtractInterfaceResult(bool succeeded, Solution updatedSolution = null, DocumentId navigationDocumentId = null)
        {
            this.Succeeded = succeeded;
            this.UpdatedSolution = updatedSolution;
            this.NavigationDocumentId = navigationDocumentId;
        }
    }
}
