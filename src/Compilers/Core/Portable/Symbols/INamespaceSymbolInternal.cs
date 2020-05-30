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

        /// <summary>
        /// Returns whether this namespace was created to be the containing symbol for an <see
        /// cref="IErrorTypeSymbol"/>.
        /// </summary>
        /// <remarks>
        /// These symbols can be created when a <see cref="Compilation"/> must create a missing type symbol and that
        /// missing symbol is from a namespace that itself was not defined within the compilation itself.  In other
        /// words, in those cases, trying to walk the symbols exposed from <see cref="Compilation.GlobalNamespace"/>
        /// will not produce the missing namespace symbols created for those types.
        /// </remarks>
        bool IsMissingNamespace { get; }
    }
}
