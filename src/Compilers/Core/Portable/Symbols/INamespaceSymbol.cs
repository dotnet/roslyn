// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a namespace.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface INamespaceSymbol : INamespaceOrTypeSymbol
    {
        /// <summary>
        /// Get all the members of this symbol.
        /// </summary>
        new IEnumerable<INamespaceOrTypeSymbol> GetMembers();

        /// <summary>
        /// Get all the members of this symbol that have a particular name.
        /// </summary>
        new IEnumerable<INamespaceOrTypeSymbol> GetMembers(string name);

        /// <summary>
        /// Get all the members of this symbol that have a particular name.
        /// </summary>
        void GetMembers<TArg>(string name, Action<INamespaceOrTypeSymbol, TArg> callback, TArg argument);

        /// <summary>
        /// Get all the members of this symbol that are namespaces.
        /// </summary>
        IEnumerable<INamespaceSymbol> GetNamespaceMembers();

        /// <summary>
        /// Returns whether this namespace is the unnamed, global namespace that is 
        /// at the root of all namespaces.
        /// </summary>
        bool IsGlobalNamespace { get; }

        /// <summary>
        /// The kind of namespace: Module, Assembly or Compilation.
        /// Module namespaces contain only members from the containing module that share the same namespace name.
        /// Assembly namespaces contain members for all modules in the containing assembly that share the same namespace name.
        /// Compilation namespaces contain all members, from source or referenced metadata (assemblies and modules) that share the same namespace name.
        /// </summary>
        NamespaceKind NamespaceKind { get; }

        /// <summary>
        /// The containing compilation for compilation namespaces.
        /// </summary>
        Compilation? ContainingCompilation { get; }

        /// <summary>
        /// If a namespace is an assembly or compilation namespace, it may be composed of multiple
        /// namespaces that are merged together. If so, ConstituentNamespaces returns
        /// all the namespaces that were merged. If this namespace was not merged, returns
        /// an array containing only this namespace.
        /// </summary>
        ImmutableArray<INamespaceSymbol> ConstituentNamespaces { get; }
    }
}
