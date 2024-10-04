// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InheritanceMargin;

internal interface IRemoteInheritanceMarginService
{
    ValueTask<ImmutableArray<InheritanceMarginItem>> GetInheritanceMarginItemsAsync(
        Checksum solutionChecksum,
        DocumentId documentId,
        TextSpan spanToSearch,
        bool includeGlobalImports,
        bool frozenPartialSemantics,
        CancellationToken cancellationToken);
}
