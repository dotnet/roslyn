﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

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

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var metadataName = reader.ReadString()!;
                var isCompilationGlobalNamespace = reader.ReadBoolean();
                var containingSymbolResolution = reader.ReadSymbolKey(out var containingSymbolFailureReason);

                if (containingSymbolFailureReason != null)
                {
                    failureReason = $"({nameof(EventSymbolKey)} {nameof(containingSymbolResolution)} failed -> {containingSymbolFailureReason})";
                    return default;
                }

                if (isCompilationGlobalNamespace)
                {
                    failureReason = null;
                    return new SymbolKeyResolution(reader.Compilation.GlobalNamespace);
                }

                using var result = PooledArrayBuilder<INamespaceSymbol>.GetInstance();
                foreach (var container in containingSymbolResolution)
                {
                    switch (container)
                    {
                        case IAssemblySymbol assembly:
                            Debug.Assert(metadataName == string.Empty);
                            result.AddIfNotNull(assembly.GlobalNamespace);
                            break;
                        case IModuleSymbol module:
                            Debug.Assert(metadataName == string.Empty);
                            result.AddIfNotNull(module.GlobalNamespace);
                            break;
                        case INamespaceSymbol namespaceSymbol:
                            foreach (var member in namespaceSymbol.GetMembers(metadataName))
                            {
                                if (member is INamespaceSymbol childNamespace)
                                {
                                    result.AddIfNotNull(childNamespace);
                                }
                            }
                            break;
                    }
                }

                return CreateResolution(result, $"({nameof(NamespaceSymbolKey)} '{metadataName}' not found)", out failureReason);
            }
        }
    }
}
