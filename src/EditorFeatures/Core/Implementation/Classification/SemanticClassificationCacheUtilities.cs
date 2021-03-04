// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PersistentStorage;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal static class SemanticClassificationCacheUtilities
    {
        public static DocumentKey GetDocumentKeyForCaching(Document document)
        {
            var project = document.Project;

            // We very intentionally persist this information against using a null 'parseOptionsChecksum'.  This way the
            // results will be valid and something we can lookup regardless of the project configuration.  In other
            // words, if we've cached the information when in the DEBUG state of the project, but we lookup when in the
            // RELEASE state, we'll still find the entry.  The data may be inaccurate, but that's ok as this is just for
            // temporary classifying until the real classifier takes over when the solution fully loads.
            var projectKey = new ProjectKey(SolutionKey.ToSolutionKey(project.Solution), project.Id, project.FilePath, project.Name, Checksum.Null);
            return new DocumentKey(projectKey, document.Id, document.FilePath, document.Name);
        }
    }
}
