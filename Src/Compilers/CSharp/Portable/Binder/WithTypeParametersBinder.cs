// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class WithTypeParametersBinder : Binder
    {
        internal WithTypeParametersBinder(Binder next)
            : base(next)
        {
        }

        // TODO: Change this to a data structure that won't allocate enumerators
        protected abstract MultiDictionary<string, TypeParameterSymbol> TypeParameterMap { get; }

        protected override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if ((options & (LookupOptions.NamespaceAliasesOnly | LookupOptions.MustBeInvocableIfMember)) != 0)
            {
                return;
            }

            Debug.Assert(result.IsClear);

            var count = TypeParameterMap.GetCountForKey(name);
            if (count == 1)
            {
                TypeParameterSymbol p;
                TypeParameterMap.TryGetSingleValue(name, out p);
                result.MergeEqual(originalBinder.CheckViability(p, arity, options, null, diagnose, ref useSiteDiagnostics));
            }
            else if (count > 1)
            {
                var parameters = TypeParameterMap[name];
                foreach (var s in parameters)
                {
                    result.MergeEqual(originalBinder.CheckViability(s, arity, options, null, diagnose, ref useSiteDiagnostics));
                }
            }
        }
    }
}