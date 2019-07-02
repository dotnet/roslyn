// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly ImmutableArray<TypeWithAnnotations> _typeArguments;
        private MultiDictionary<string, TypeParameterSymbol> _lazyTypeParameterMap;

        internal WithTypeArgumentsBinder(ImmutableArray<TypeWithAnnotations> typeArguments, Binder next)
            : base(next)
        {
            Debug.Assert(!typeArguments.IsDefaultOrEmpty);
            Debug.Assert(typeArguments.All(ta => ta.Type.Kind == SymbolKind.TypeParameter));
            _typeArguments = typeArguments;
        }

        protected override MultiDictionary<string, TypeParameterSymbol> TypeParameterMap
        {
            get
            {
                if (_lazyTypeParameterMap == null)
                {
                    var result = new MultiDictionary<string, TypeParameterSymbol>();
                    foreach (var tps in _typeArguments)
                    {
                        result.Add(tps.Type.Name, (TypeParameterSymbol)tps.Type);
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
                    if (originalBinder.CanAddLookupSymbolInfo(parameter.Type, options, result, null))
                    {
                        result.AddSymbol(parameter.Type, parameter.Type.Name, 0);
                    }
                }
            }
        }
    }
}
