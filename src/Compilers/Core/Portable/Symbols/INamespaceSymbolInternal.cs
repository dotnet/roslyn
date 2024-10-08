// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface INamespaceSymbolInternal : INamespaceOrTypeSymbolInternal
    {
        /// <summary>
        /// Returns whether this namespace is the unnamed, global namespace that is 
        /// at the root of all namespaces.
        /// </summary>
        bool IsGlobalNamespace { get; }
    }
}
