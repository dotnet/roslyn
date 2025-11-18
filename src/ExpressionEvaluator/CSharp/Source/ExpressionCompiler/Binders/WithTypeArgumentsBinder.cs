// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

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

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
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
