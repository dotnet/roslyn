// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

/// <summary>
/// Manages persisting metadata documents (outside of the workspace).
/// For example, it can persist generated metadata documents to disk or handle them in memory.
/// </summary>
internal interface IMetadataDocumentPersister
{
    /// <summary>
    /// Generates a path to use to persist a generated metadata document.
    /// This can be a path on disk or URI pointing to an in-memory document.
    /// </summary>
    /// <param name="identifier">a unique identifier for the file being generated</param>
    /// <param name="providerName">the provider generating the file</param>
    /// <param name="fileName">the file name generated</param>
    string GenerateDocumentPath(Guid identifier, string providerName, string fileName);

    /// <summary>
    /// Given a file path on disk, convert it to the representation that the persister uses for generated metadata documents.
    /// In the case of a file on disk, this can be the exact same file path, or it might be a different URI for virtual documents.
    /// </summary>
    string ConvertFilePathToDocumentPath(Guid identifier, string providerName, string filePath);

    /// <summary>
    /// Tries to get existing persisted text for a document path that was generated previously.
    /// </summary>
    Task<SourceText?> TryGetExistingTextAsync(string documentPath, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, Func<SourceText, bool> verifyExistingDocument, CancellationToken cancellationToken);

    /// <summary>
    /// Writes the generated metadata file to a persistent location.
    /// </summary>
    Task<bool> WriteMetadataDocumentAsync(string documentFilePath, Encoding encoding, SourceText text, Action<Exception>? logFailure, CancellationToken cancellationToken);

    /// <summary>
    /// Cleans up documents written by this persister.
    /// </summary>
    void CleanupGeneratedDocuments();
}
