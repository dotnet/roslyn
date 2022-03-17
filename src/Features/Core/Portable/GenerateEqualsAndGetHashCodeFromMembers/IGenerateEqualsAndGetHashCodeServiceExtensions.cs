// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
{
    internal static class IGenerateEqualsAndGetHashCodeServiceExtensions
    {
        public static Task<IMethodSymbol> GenerateEqualsMethodAsync(
            this IGenerateEqualsAndGetHashCodeService service, Document document, INamedTypeSymbol namedType,
            ImmutableArray<ISymbol> members, CancellationToken cancellationToken)
            => service.GenerateEqualsMethodAsync(document, namedType, members, localNameOpt: null, cancellationToken);
    }
}
