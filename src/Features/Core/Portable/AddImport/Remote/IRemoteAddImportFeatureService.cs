// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal interface IRemoteAddImportFeatureService
    {
        Task<IList<AddImportFixData>> GetFixesAsync(
            DocumentId documentId, TextSpan span, string diagnosticId, int maxResults, bool placeSystemNamespaceFirst,
            bool searchReferenceAssemblies, IList<PackageSource> packageSources, CancellationToken cancellationToken);
    }
}
