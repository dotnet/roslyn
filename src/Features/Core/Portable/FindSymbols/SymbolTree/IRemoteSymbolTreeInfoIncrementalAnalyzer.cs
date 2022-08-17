// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols.SymbolTree
{
    internal interface IRemoteSymbolTreeInfoIncrementalAnalyzer
    {
        ValueTask AnalyzeDocumentAsync(Checksum solutionChecksum, DocumentId documentId, bool isMethodBodyEdit, CancellationToken cancellationToken);

        ValueTask AnalyzeProjectAsync(Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken);

        ValueTask RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken);
    }
}
