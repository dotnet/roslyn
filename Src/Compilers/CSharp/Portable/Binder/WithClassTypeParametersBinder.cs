// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that places class/interface/struct/delegate type parameters in scope
    /// </summary>
    internal sealed class WithClassTypeParametersBinder : WithTypeParametersBinder
    {
        private readonly NamedTypeSymbol namedType;
        private MultiDictionary<string, TypeParameterSymbol> lazyTypeParameterMap;

        internal WithClassTypeParametersBinder(NamedTypeSymbol container, Binder next)
            : base(next)
        {
            Debug.Assert((object)container != null);
            this.namedType = container;
        }

        internal override bool IsAccessible(Symbol symbol, TypeSymbol accessThroughType, out bool failedThroughTypeCheck, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<Symbol> basesBeingResolved = null)
        {
            return this.IsSymbolAccessibleConditional(symbol, namedType, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics, basesBeingResolved);
        }

        protected override MultiDictionary<string, TypeParameterSymbol> TypeParameterMap
        {
            get
            {
                if (this.lazyTypeParameterMap == null)
                {
                    var result = new MultiDictionary<string, TypeParameterSymbol>();
                    foreach (TypeParameterSymbol tps in this.namedType.TypeParameters)
                    {
                        result.Add(tps.Name, tps);
                    }
                    Interlocked.CompareExchange(ref this.lazyTypeParameterMap, result, null);
                }
                return lazyTypeParameterMap;
            }
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (options.CanConsiderTypeParameters())
            {
                foreach (var parameter in this.namedType.TypeParameters)
                {
                    if (originalBinder.CanAddLookupSymbolInfo(parameter, options, null))
                    {
                        result.AddSymbol(parameter, parameter.Name, 0);
                    }
                }
            }
        }
    }
}
