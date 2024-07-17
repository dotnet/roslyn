// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Classification;

internal static class SemanticClassificationCacheUtilities
{
    public static async Task<(DocumentKey documentKey, Checksum checksum)> GetDocumentKeyAndChecksumAsync(
        Document document, CancellationToken cancellationToken)
    {
        var project = document.Project;

        // We very intentionally persist this information against using a null 'parseOptionsChecksum'.  This way the
        // results will be valid and something we can lookup regardless of the project configuration.  In other
        // words, if we've cached the information when in the DEBUG state of the project, but we lookup when in the
        // RELEASE state, we'll still find the entry.  The data may be inaccurate, but that's ok as this is just for
        // temporary classifying until the real classifier takes over when the solution fully loads.
        var projectKey = new ProjectKey(SolutionKey.ToSolutionKey(project.Solution), project.Id, project.FilePath, project.Name, Checksum.Null);
        var documentKey = new DocumentKey(projectKey, document.Id, document.FilePath, document.Name);

        // We only checksum off of the contents of the file.  During load, we can't really compute any other
        // information since we don't necessarily know about other files, metadata, or dependencies.  So during
        // load, we allow for the previous semantic classifications to be used as long as the file contents match.
        var checksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

        return (documentKey, checksums.Text);
    }
}
