// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal interface IRemoteAddImportFeatureService
    {
        Task<SerializableAddImportFixData[]> GetFixesAsync(
            DocumentId documentId, TextSpan span, string diagnosticId,
            bool searchReferenceAssemblies, PackageSource[] packageSources);
    }
}