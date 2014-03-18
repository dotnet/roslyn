// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class WithLambdaParametersBinder : LocalScopeBinder
    {
        protected readonly LambdaSymbol lambdaSymbol;
        protected readonly MultiDictionary<string, ParameterSymbol> parameterMap;

        public WithLambdaParametersBinder(LambdaSymbol lambdaSymbol, Binder enclosing)
            : base(lambdaSymbol, enclosing)
        {
            ForceSingleDefinitions(lambdaSymbol.Parameters);
            this.lambdaSymbol = lambdaSymbol;
            this.parameterMap = new MultiDictionary<string, ParameterSymbol>();
            foreach (var parameter in lambdaSymbol.Parameters)
            {
                this.parameterMap.Add(parameter.Name, parameter);
            }
        }

        protected override TypeSymbol GetCurrentReturnType()
        {
            return lambdaSymbol.ReturnType;
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get
            {
                return this.lambdaSymbol;
            }
        }

        internal override bool IsDirectlyInIterator
        {
            get
            {
                return false;
            }
        }

        // NOTE: Specifically not overriding IsIndirectlyInIterator.

        internal override TypeSymbol GetIteratorElementType(YieldStatementSyntax node, DiagnosticBag diagnostics)
        {
            if (node != null) diagnostics.Add(ErrorCode.ERR_YieldInAnonMeth, node.YieldKeyword.GetLocation());
            return CreateErrorType();
        }

        protected override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if ((options & LookupOptions.NamespaceAliasesOnly) != 0) return;

            Debug.Assert(result.IsClear);

            var count = parameterMap.GetCountForKey(name);
            if (count == 1)
            {
                ParameterSymbol p;
                parameterMap.TryGetSingleValue(name, out p);
                result.MergeEqual(originalBinder.CheckViability(p, arity, options, null, diagnose, ref useSiteDiagnostics));
            }
            else if (count > 1)
            {
                foreach (var sym in parameterMap[name])
                {
                    result.MergeEqual(originalBinder.CheckViability(sym, arity, options, null, diagnose, ref useSiteDiagnostics));
                }
            }
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (options.CanConsiderMembers())
            {
                foreach (var parameter in lambdaSymbol.Parameters)
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