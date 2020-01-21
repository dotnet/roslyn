// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

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

                using var result = PooledArrayBuilder<IParameterSymbol>.GetInstance();
                foreach (var container in containingSymbolResolution)
                {
                    switch (container)
                    {
                        case IMethodSymbol method:
                            Resolve(result, reader, metadataName, method.Parameters);
                            break;
                        case IPropertySymbol property:
                            Resolve(result, reader, metadataName, property.Parameters);
                            break;
                        case IEventSymbol eventSymbol:
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
                                Resolve(result, reader, metadataName, delegateInvoke.Parameters);
                            }
                            break;
                    }
                }

                return CreateResolution(result);
            }

            private static void Resolve(
                PooledArrayBuilder<IParameterSymbol> result, SymbolKeyReader reader,
                string metadataName, ImmutableArray<IParameterSymbol> parameters)
            {
                foreach (var parameter in parameters)
                {
                    if (SymbolKey.Equals(reader.Compilation, parameter.MetadataName, metadataName))
                    {
                        result.AddIfNotNull(parameter);
                    }
                }
            }
        }
    }
}
