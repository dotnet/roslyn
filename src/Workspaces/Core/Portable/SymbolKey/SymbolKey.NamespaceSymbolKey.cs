// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class NamespaceSymbolKey
        {
            // The containing symbol can be one of many things. 
            // 1) Null when this is the global namespace for a compilation.  
            // 2) The SymbolId for an assembly symbol if this is the global namespace for an
            //    assembly.
            // 3) The SymbolId for a module symbol if this is the global namespace for a module.
            // 4) The SymbolId for the containing namespace symbol if this is not a global
            //    namespace.

            public static void Create(INamespaceSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.MetadataName);

                if (symbol.ContainingNamespace != null)
                {
                    visitor.WriteBoolean(false);
                    visitor.WriteSymbolKey(symbol.ContainingNamespace);
                }
                else
                {
                    // A global namespace can either belong to a module or to a compilation.
                    Debug.Assert(symbol.IsGlobalNamespace);
                    switch (symbol.NamespaceKind)
                    {
                        case NamespaceKind.Module:
                            visitor.WriteBoolean(false);
                            visitor.WriteSymbolKey(symbol.ContainingModule);
                            break;
                        case NamespaceKind.Assembly:
                            visitor.WriteBoolean(false);
                            visitor.WriteSymbolKey(symbol.ContainingAssembly);
                            break;
                        case NamespaceKind.Compilation:
                            visitor.WriteBoolean(true);
                            visitor.WriteSymbolKey(null);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }

            public static int GetHashCode(GetHashCodeReader reader)
            {
                return Hash.Combine(reader.ReadString(),
                       Hash.Combine(reader.ReadBoolean(),
                                    reader.ReadSymbolKey()));
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var isCompilationGlobalNamespace = reader.ReadBoolean();
                var containingSymbolResolution = reader.ReadSymbolKey();

                if (isCompilationGlobalNamespace)
                {
                    return new SymbolKeyResolution(reader.Compilation.GlobalNamespace);
                }

                var namespaces = GetAllSymbols(containingSymbolResolution).SelectMany(
                    s => Resolve(s, metadataName));

                return CreateSymbolInfo(namespaces);
            }

            private static IEnumerable<INamespaceSymbol> Resolve(ISymbol container, string metadataName)
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
                    return ((INamespaceSymbol)container).GetMembers(metadataName).OfType<INamespaceSymbol>();
                }
                else
                {
                    return SpecializedCollections.EmptyEnumerable<INamespaceSymbol>();
                }
            }
        }
    }
}