// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    public interface IDocumentRefactoringService : IWorkspaceService
    {
        /// <summary>
        /// Attempts to sync a named type name and namespace based on file path. PreviousFilePath is 
        /// used to determine how the policy might be applied, assuming that matching types with previous
        /// indicates a user wants the type to match with the new location or name.
        /// May fail if:
        /// - The appropriate type for naming policy cannot be found
        /// - The type is partial and spread across multiple files
        /// - The solution changes during the operation
        /// </summary>
        /// <returns> 
        /// Returns false if the symbol update failed, true if it succeeded
        /// </returns>
        Task<bool> TryUpdateNamedTypeToMatchFilePath(DocumentId documentId, string previousFilePath, CancellationToken cancellation = default);
    }
}