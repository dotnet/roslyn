// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MetadataAsSource;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.MetadataAsSource
{
    internal static class OmniSharpMetadataAsSourceHelpers
    {
        public static string GetAssemblyInfo(IAssemblySymbol assemblySymbol)
            => MetadataAsSourceHelpers.GetAssemblyInfo(assemblySymbol);

        public static string GetAssemblyDisplay(Compilation compilation, IAssemblySymbol assemblySymbol)
            => MetadataAsSourceHelpers.GetAssemblyDisplay(compilation, assemblySymbol);

        public static Task<Location> GetLocationInGeneratedSourceAsync(ISymbol symbol, Document generatedDocument, CancellationToken cancellationToken)
        {
            var symbolKey = SymbolKey.Create(symbol, cancellationToken);
            return MetadataAsSourceHelpers.GetLocationInGeneratedSourceAsync(symbolKey, generatedDocument, cancellationToken);
        }
    }
}
