﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class WithTypeArgumentsBinder : WithTypeParametersBinder
    {
        private readonly ImmutableArray<TypeSymbol> _typeArguments;
        private MultiDictionary<string, TypeParameterSymbol> _lazyTypeParameterMap;

        internal WithTypeArgumentsBinder(ImmutableArray<TypeSymbol> typeArguments, Binder next)
            : base(next)
        {
            Debug.Assert(!typeArguments.IsDefaultOrEmpty);
            Debug.Assert(typeArguments.All(ta => ta.Kind == SymbolKind.TypeParameter));
            _typeArguments = typeArguments;
        }

        protected override MultiDictionary<string, TypeParameterSymbol> TypeParameterMap
        {
            get
            {
                if (_lazyTypeParameterMap == null)
                {
                    var result = new MultiDictionary<string, TypeParameterSymbol>();
                    foreach (TypeParameterSymbol tps in _typeArguments)
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
                foreach (var parameter in _typeArguments)
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
