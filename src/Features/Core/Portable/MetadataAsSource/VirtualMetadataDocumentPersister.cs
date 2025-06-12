// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal class VirtualMetadataDocumentPersister() : IMetadataDocumentPersister
{
    public const string VirtualFileScheme = "roslyn-metadata";
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

    public Task<SourceText?> TryGetExistingTextAsync(string documentPath, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, Func<SourceText, bool> verifyExistingDocument, CancellationToken cancellationToken)
    {
        // Nothing to do - this document is only persisted in memory by the workspace.  If the workspace does not have it, we can't retrieve it.
        return SpecializedTasks.Null<SourceText>();
    }
    public Task<bool> WriteMetadataDocumentAsync(string documentPath, Encoding encoding, SourceText text, Action<Exception>? logFailure, CancellationToken cancellationToken)
    {
        // Nothing to do - this document is an in-memory only document.  It relies on the workspace to store the text for it.
        return SpecializedTasks.True;
    }
}
