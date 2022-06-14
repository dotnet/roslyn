// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    internal interface IImplementationAssemblyLookupService
    {
        /// <summary>
        /// Uses various heuristics to try to find the implementation assembly for a reference assembly without
        /// loading 
        /// </summary>
        bool TryFindImplementationAssemblyPath(string referencedDllPath, [NotNullWhen(true)] out string? implementationDllPath);

        /// <summary>
        /// Given an implementation assembly path, follows any type forwards that might be in place
        /// for the containing type of <paramref name="symbol"/>, to ensure the right implementation
        /// assembly will be found.
        /// </summary>
        /// <remarks>
        /// To avoid mutiple reads of a single DLL this method caches all type forwards found in any
        /// DLL it loads.
        /// </remarks>
        string? FollowTypeForwards(ISymbol symbol, string dllPath, IPdbSourceDocumentLogger? logger);

        /// <summary>
        /// Clears any cached type forward information
        /// </summary>
        void Clear();
    }
}
