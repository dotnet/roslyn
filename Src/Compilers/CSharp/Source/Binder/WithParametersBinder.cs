// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class WithParametersBinder : Binder
    {
        private readonly ImmutableArray<ParameterSymbol> parameters;

        internal WithParametersBinder(ImmutableArray<ParameterSymbol> parameters, Binder next)
            : base(next)
        {
            Debug.Assert(!parameters.IsEmpty);
            this.parameters = parameters;
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (options.CanConsiderLocals())
            {
                foreach (var parameter in this.parameters)
                {
                    if (originalBinder.CanAddLookupSymbolInfo(parameter, options, null))
                    {
                        result.AddSymbol(parameter, parameter.Name, 0);
                    }
                }
            }
        }

        protected override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if ((options & (LookupOptions.NamespaceAliasesOnly | LookupOptions.MustBeInvocableIfMember)) != 0)
            {
                return;
            }

            Debug.Assert(result.IsClear);

            foreach (ParameterSymbol parameter in parameters)
            {
                if (parameter.Name == name)
                {
                    result.MergeEqual(originalBinder.CheckViability(parameter, arity, options, null, diagnose, ref useSiteDiagnostics));
                }
            }
        }
    }
}