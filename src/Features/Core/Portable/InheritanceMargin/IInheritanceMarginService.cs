// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    internal interface IInheritanceMarginService : ILanguageService
    {
        /// <summary>
        /// Get information about
        /// 1. The inheritance chain of the member in given span.
        /// 2. The global imports for the document.
        /// </summary>
        ValueTask<ImmutableArray<InheritanceMarginItem>> GetInheritanceMemberItemsAsync(
            Document document,
            TextSpan spanToSearch,
            bool includeGlobalImports,
            bool frozenPartialSemantics,
            CancellationToken cancellationToken);
    }
}
