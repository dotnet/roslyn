// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial struct SymbolKey
    {
        private static class ParameterSymbolKey
        {
            public static void Create(IParameterSymbol symbol, SymbolKeyWriter writer)
            {
                writer.WriteString(symbol.MetadataName);
                writer.WriteSymbolKey(symbol.ContainingSymbol);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var resolvedContainingSymbol = reader.ReadSymbolKey();

                var parameters = GetParameterSymbols(metadataName, resolvedContainingSymbol, reader.Compilation);

                return SymbolKeyResolution.Create(parameters);
            }

            private static ImmutableArray<IParameterSymbol> GetParameterSymbols(string metadataName, SymbolKeyResolution resolvedContainingSymbol, Compilation compilation)
            {
                var result = ArrayBuilder<IParameterSymbol>.GetInstance();

                foreach (var containingSymbol in resolvedContainingSymbol.GetAllSymbols())
                {
                    if (containingSymbol is IMethodSymbol methodSymbol)
                    {
                        AddParameters(methodSymbol.Parameters);
                    }
                    else if (containingSymbol is IPropertySymbol propertySymbol)
                    {
                        AddParameters(propertySymbol.Parameters);
                    }
                    else if (containingSymbol is IEventSymbol eventSymbol)
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
                            AddParameters(delegateInvoke.Parameters);
                        }
                    }
                }

                void AddParameters(ImmutableArray<IParameterSymbol> parameters)
                {
                    foreach (var parameter in parameters)
                    {
                        if (NamesAreEqual(compilation, parameter.MetadataName, metadataName))
                        {
                            result.Add(parameter);
                        }
                    }
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
