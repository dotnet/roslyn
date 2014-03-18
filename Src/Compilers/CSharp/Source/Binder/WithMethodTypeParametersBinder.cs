// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that places method type parameters in scope.
    /// </summary>
    internal sealed class WithMethodTypeParametersBinder : WithTypeParametersBinder
    {
        private readonly MethodSymbol methodSymbol;
        private MultiDictionary<string, TypeParameterSymbol> lazyTypeParameterMap;

        internal WithMethodTypeParametersBinder(MethodSymbol methodSymbol, Binder next)
            : base(next)
        {
            this.methodSymbol = methodSymbol;
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get
            {
                return this.methodSymbol;
            }
        }

        protected override MultiDictionary<string, TypeParameterSymbol> TypeParameterMap
        {
            get
            {
                if (this.lazyTypeParameterMap == null)
                {
                    var result = new MultiDictionary<string, TypeParameterSymbol>();
                    foreach (var typeParameter in this.methodSymbol.TypeParameters)
                    {
                        result.Add(typeParameter.Name, typeParameter);
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
                foreach (var parameter in this.methodSymbol.TypeParameters)
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
