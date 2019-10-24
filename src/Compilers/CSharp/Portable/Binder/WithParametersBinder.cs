// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Binder used to place the parameters of a method, property, indexer, or delegate
    /// in scope when binding &lt;param&gt; tags inside of XML documentation comments.
    /// </summary>
    internal sealed class WithParametersBinder : Binder
    {
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        internal WithParametersBinder(ImmutableArray<ParameterSymbol> parameters, Binder next)
            : base(next)
        {
            Debug.Assert(!parameters.IsDefaultOrEmpty);
            _parameters = parameters;
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (options.CanConsiderLocals())
            {
                foreach (var parameter in _parameters)
                {
                    if (originalBinder.CanAddLookupSymbolInfo(parameter, options, result, null))
                    {
                        result.AddSymbol(parameter, parameter.Name, 0);
                    }
                }
            }
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if ((options & (LookupOptions.NamespaceAliasesOnly | LookupOptions.MustBeInvocableIfMember)) != 0)
            {
                return;
            }

            Debug.Assert(result.IsClear);

            foreach (ParameterSymbol parameter in _parameters)
            {
                if (parameter.Name == name && !parameter.IsDiscard)
                {
                    result.MergeEqual(originalBinder.CheckViability(parameter, arity, options, null, diagnose, ref useSiteDiagnostics));
                }
            }
        }
    }
}
