﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly NamedTypeSymbol _namedType;
        private MultiDictionary<string, TypeParameterSymbol> _lazyTypeParameterMap;

        internal WithClassTypeParametersBinder(NamedTypeSymbol container, Binder next)
            : base(next)
        {
            Debug.Assert((object)container != null);
            _namedType = container;
        }

        internal override bool IsAccessibleHelper(Symbol symbol, TypeSymbol accessThroughType, out bool failedThroughTypeCheck, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<TypeSymbol> basesBeingResolved)
        {
            return this.IsSymbolAccessibleConditional(symbol, _namedType, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics, basesBeingResolved);
        }

        protected override MultiDictionary<string, TypeParameterSymbol> TypeParameterMap
        {
            get
            {
                if (_lazyTypeParameterMap == null)
                {
                    var result = new MultiDictionary<string, TypeParameterSymbol>();
                    foreach (TypeParameterSymbol tps in _namedType.TypeParameters)
                    {
                        result.Add(tps.Name, tps);
                    }
                    Interlocked.CompareExchange(ref _lazyTypeParameterMap, result, null);
                }
                return _lazyTypeParameterMap;
            }
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (CanConsiderTypeParameters(options))
            {
                foreach (var parameter in _namedType.TypeParameters)
                {
                    if (originalBinder.CanAddLookupSymbolInfo(parameter, options, result, null))
                    {
                        result.AddSymbol(parameter, parameter.Name, 0);
                    }
                }
            }
        }
    }
}
