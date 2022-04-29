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
        /// Get the lines need to be have a margin and the member's information on that line.
        /// </summary>
        ValueTask<ImmutableArray<InheritanceMarginItem>> GetInheritanceMemberItemsAsync(
            Document document,
            TextSpan spanToSearch,
            bool includeGlobalImports,
            CancellationToken cancellationToken);
    }
}
