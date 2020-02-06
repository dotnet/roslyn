﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeLens
{
    internal interface IRemoteCodeLensReferencesService
    {
        Task<ReferenceCount> GetReferenceCountAsync(DocumentId documentId, TextSpan textSpan, int maxResultCount, CancellationToken cancellationToken);
        Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);
        Task<IEnumerable<ReferenceMethodDescriptor>> FindReferenceMethodsAsync(DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);
        Task<string> GetFullyQualifiedName(DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);

        Task TrackCodeLensAsync(DocumentId documentId, CancellationToken cancellationToken);
    }

    internal interface IRemoteCodeLensDataPoint
    {
        void Invalidate();
    }
}
