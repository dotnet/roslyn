// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
