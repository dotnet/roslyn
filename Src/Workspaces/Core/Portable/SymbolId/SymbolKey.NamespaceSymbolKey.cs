// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class NamespaceSymbolKey : AbstractSymbolKey<NamespaceSymbolKey>
        {
            // This can be one of many things. 
            // 1) Null when this is the global namespace for a compilation.  
            // 2) The SymbolId for an assembly symbol if this is the global namespace for an
            //    assembly.
            // 3) The SymbolId for a module symbol if this is the global namespace for a module.
            // 4) The SymbolId for the containing namespace symbol if this is not a global
            //    namespace.
            private readonly SymbolKey containerKeyOpt;
            private readonly string metadataName;

            internal NamespaceSymbolKey(INamespaceSymbol symbol, Visitor visitor)
            {
                this.containerKeyOpt = DetermineContainerKey(symbol, visitor);
                this.metadataName = symbol.MetadataName;
            }

            private SymbolKey DetermineContainerKey(INamespaceSymbol symbol, Visitor visitor)
            {
                if (symbol.ContainingNamespace != null)
                {
                    return GetOrCreate(symbol.ContainingNamespace, visitor);
                }
                else
                {
                    // A global namespace can either belong to a module or to a compilation.
                    Debug.Assert(symbol.IsGlobalNamespace);
                    switch (symbol.NamespaceKind)
                    {
                        case NamespaceKind.Module:
                            return GetOrCreate(symbol.ContainingModule, visitor);
                        case NamespaceKind.Assembly:
                            return GetOrCreate(symbol.ContainingAssembly, visitor);
                        case NamespaceKind.Compilation:
                            // Store nothing in this case.
                            break;
                    }

                    return null;
                }
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                if (ReferenceEquals(this.containerKeyOpt, null))
                {
                    return new SymbolKeyResolution(compilation.GlobalNamespace);
                }

                var container = containerKeyOpt.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var namespaces = GetAllSymbols(container).SelectMany(s => Resolve(compilation, s, ignoreAssemblyKey));

                return CreateSymbolInfo(namespaces);
            }

            private IEnumerable<INamespaceSymbol> Resolve(Compilation compilation, ISymbol container, bool ignoreAssemblyKey)
            {
                if (container is IAssemblySymbol)
                {
                    Debug.Assert(metadataName == string.Empty);
                    return SpecializedCollections.SingletonEnumerable(((IAssemblySymbol)container).GlobalNamespace);
                }
                else if (container is IModuleSymbol)
                {
                    Debug.Assert(metadataName == string.Empty);
                    return SpecializedCollections.SingletonEnumerable(((IModuleSymbol)container).GlobalNamespace);
                }
                else if (container is INamespaceSymbol)
                {
                    return ((INamespaceSymbol)container).GetMembers(this.metadataName).OfType<INamespaceSymbol>();
                }
                else
                {
                    return SpecializedCollections.EmptyEnumerable<INamespaceSymbol>();
                }
            }

            internal override bool Equals(NamespaceSymbolKey other, ComparisonOptions options)
            {
                var comparer = SymbolKeyComparer.GetComparer(options);
                return
                    Equals(options.IgnoreCase, other.metadataName, this.metadataName) &&
                    comparer.Equals(other.containerKeyOpt, this.containerKeyOpt);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                var comparer = SymbolKeyComparer.GetComparer(options);
                return Hash.Combine(
                    GetHashCode(options.IgnoreCase, this.metadataName),
                    comparer.GetHashCode(this.containerKeyOpt));
            }
        }
    }
}