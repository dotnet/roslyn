// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal class WorkspaceMetadataDocumentPersister : IMetadataDocumentPersister
{
    public const string VirtualFileScheme = "roslyn-metadata";

    private readonly Workspace _workspace;
    public WorkspaceMetadataDocumentPersister(Workspace workspace)
    {
        _workspace = workspace;
    }

    public void CleanupGeneratedDocuments()
    {
        // Documents will be cleaned up by the workspace, nothing else we need to do here.
        return;
    }

    public string ConvertFilePathToDocumentPath(Guid identifier, string providerName, string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return GenerateDocumentPath(identifier, providerName, fileName);
    }

    public string GenerateDocumentPath(Guid identifier, string providerName, string fileName)
    {
        return $"{VirtualFileScheme}://{providerName}/{identifier:N}/{fileName}";
    }

    public async Task<SourceText?> TryGetExistingTextAsync(string documentPath, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, Func<SourceText, bool> verifyExistingDocument, CancellationToken cancellationToken)
    {
        var solution = _workspace.CurrentSolution;
        var documentId = solution.GetDocumentIdsWithFilePath(documentPath).SingleOrDefault();
        if (documentId is null)
            // No document with this path exists in the workspace.
            return null;

        var document = solution.GetRequiredDocument(documentId);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return text;
    }
    public Task<bool> WriteMetadataFileAsync(string documentPath, Encoding encoding, SourceText text, Action<Exception>? logFailure, CancellationToken cancellationToken)
    {
        // Nothing to do - this document is an in-memory only document.  It relies on the workspace to store the text for it.
        return SpecializedTasks.True;
    }
}
