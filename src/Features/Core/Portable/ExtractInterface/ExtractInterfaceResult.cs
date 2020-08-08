// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal sealed class ExtractInterfaceResult
    {
        public bool Succeeded { get; }
        public Solution UpdatedSolution { get; }
        public DocumentId NavigationDocumentId { get; }

        public ExtractInterfaceResult(bool succeeded, Solution updatedSolution = null, DocumentId navigationDocumentId = null)
        {
            Succeeded = succeeded;
            UpdatedSolution = updatedSolution;
            NavigationDocumentId = navigationDocumentId;
        }
    }
}
