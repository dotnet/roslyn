// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal sealed class ExtractInterfaceResult(bool succeeded, Solution updatedSolution = null, DocumentId navigationDocumentId = null)
    {
        public bool Succeeded { get; } = succeeded;
        public Solution UpdatedSolution { get; } = updatedSolution;
        public DocumentId NavigationDocumentId { get; } = navigationDocumentId;
    }
}
