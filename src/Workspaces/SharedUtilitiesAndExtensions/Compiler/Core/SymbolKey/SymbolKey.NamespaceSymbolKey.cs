// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class NamespaceSymbolKey : AbstractSymbolKey<INamespaceSymbol>
    {
        public static readonly NamespaceSymbolKey Instance = new();

        // The containing symbol can be one of many things. 
        // 1) Null when this is the global namespace for a compilation.  
        // 2) The SymbolId for an assembly symbol if this is the global namespace for an
        //    assembly.
        // 3) The SymbolId for a module symbol if this is the global namespace for a module.
        // 4) The SymbolId for the containing namespace symbol if this is not a global
        //    namespace.

        public sealed override void Create(INamespaceSymbol symbol, SymbolKeyWriter visitor)
        {
            visitor.WriteString(symbol.MetadataName);

            if (symbol.ContainingNamespace != null)
            {
                visitor.WriteInteger(0);
                visitor.WriteSymbolKey(symbol.ContainingNamespace);
            }
            else
            {
                // A global namespace can either belong to a module or to a compilation.
                Debug.Assert(symbol.IsGlobalNamespace);
                switch (symbol.NamespaceKind)
                {
                    case NamespaceKind.Module:
                        visitor.WriteInteger(1);
                        visitor.WriteSymbolKey(symbol.ContainingModule);
                        break;
                    case NamespaceKind.Assembly:
                        visitor.WriteInteger(2);
                        visitor.WriteSymbolKey(symbol.ContainingAssembly);
                        break;
                    case NamespaceKind.Compilation:
                        visitor.WriteInteger(3);
                        visitor.WriteSymbolKey(null);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, INamespaceSymbol? contextualSymbol, out string? failureReason)
        {
            var metadataName = reader.ReadRequiredString();
            var containerKind = reader.ReadInteger();

            var containingContextualSymbol = containerKind switch
            {
                0 => contextualSymbol?.ContainingNamespace,
                1 => contextualSymbol?.ContainingModule,
                2 => contextualSymbol?.ContainingAssembly,
                3 => (ISymbol?)null,
                _ => throw ExceptionUtilities.UnexpectedValue(containerKind),
            };

            // Namespaces are never parented by types, so there can be no contextual type to resolve our container.
            var containingSymbolResolution = reader.ReadSymbolKey(
                containingContextualSymbol, out var containingSymbolFailureReason);

            if (containingSymbolFailureReason != null)
            {
                failureReason = $"({nameof(NamespaceSymbolKey)} {nameof(containingSymbolResolution)} failed -> {containingSymbolFailureReason})";
                return default;
            }

            if (containerKind == 3)
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
