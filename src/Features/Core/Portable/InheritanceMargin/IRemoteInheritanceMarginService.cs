// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal interface IRemoteInheritanceMarginService
    {
        ValueTask<ImmutableArray<InheritanceMarginItem>> GetGlobalImportItemsAsync(
            Checksum solutionChecksum,
            DocumentId documentId,
            TextSpan spanToSearch,
            bool frozenPartialSemantics,
            CancellationToken cancellationToken);

        ValueTask<ImmutableArray<InheritanceMarginItem>> GetSymbolItemsAsync(
            Checksum solutionChecksum,
            ProjectId projectId,
            DocumentId? documentId,
            ImmutableArray<(SymbolKey symbolKey, int lineNumber)> symbolKeyAndLineNumbers,
            bool frozenPartialSemantics,
            CancellationToken cancellationToken);
    }
}
