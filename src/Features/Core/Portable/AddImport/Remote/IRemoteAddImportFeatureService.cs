// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal interface IRemoteAddImportFeatureService
    {
        Task<IList<AddImportFixData>> GetFixesAsync(
            PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan span, string diagnosticId, int maxResults, bool placeSystemNamespaceFirst,
            bool searchReferenceAssemblies, IList<PackageSource> packageSources, CancellationToken cancellationToken);
    }
}
