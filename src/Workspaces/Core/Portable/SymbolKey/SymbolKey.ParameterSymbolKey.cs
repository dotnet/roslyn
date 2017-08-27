// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class ParameterSymbolKey
        {
            public static void Create(IParameterSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.MetadataName);
                visitor.WriteSymbolKey(symbol.ContainingSymbol);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();

                var parameters = GetAllSymbols(containingSymbolResolution).SelectMany(
                    s => Resolve(reader, s, metadataName));
                return CreateSymbolInfo(parameters);
            }

            private static IEnumerable<IParameterSymbol> Resolve(
                SymbolKeyReader reader, ISymbol container, string metadataName)
            {
                if (container is IMethodSymbol method)
                {
                    return method.Parameters.Where(
                        p => SymbolKey.Equals(reader.Compilation, p.MetadataName, metadataName));
                }
                else if (container is IPropertySymbol property)
                {
                    return property.Parameters.Where(
                        p => SymbolKey.Equals(reader.Compilation, p.MetadataName, metadataName));
                }
                else if (container is IEventSymbol eventSymbol)
                {
                    // Parameters can be owned by events in VB.  i.e. it's legal in VB to have:
                    //
                    //      Public Event E(a As Integer, b As Integer);
                    //
                    // In this case it's equivalent to:
                    //
                    //      Public Delegate UnutterableCompilerName(a As Integer, b As Integer)
                    //      public Event E As UnutterableCompilerName
                    //
                    // So, in this case, to resolve the parameter, we go have to map the event,
                    // then find the delegate it returns, then find the parameter in the delegate's
                    // 'Invoke' method.
                    var delegateInvoke = (eventSymbol.Type as INamedTypeSymbol)?.DelegateInvokeMethod;

                    if (delegateInvoke != null)
                    {
                        return delegateInvoke.Parameters.Where(
                            p => SymbolKey.Equals(reader.Compilation, p.MetadataName, metadataName));
                    }
                }

                return SpecializedCollections.EmptyEnumerable<IParameterSymbol>();
            }
        }
    }
}
