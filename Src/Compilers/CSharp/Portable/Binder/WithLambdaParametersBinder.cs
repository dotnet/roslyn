// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class WithLambdaParametersBinder : LocalScopeBinder
    {
        protected readonly LambdaSymbol lambdaSymbol;
        protected readonly MultiDictionary<string, ParameterSymbol> parameterMap;
        private SmallDictionary<string, ParameterSymbol> definitionMap;

        public WithLambdaParametersBinder(LambdaSymbol lambdaSymbol, Binder enclosing)
            : base(enclosing)
        {
            this.lambdaSymbol = lambdaSymbol;
            this.parameterMap = new MultiDictionary<string, ParameterSymbol>();

            var parameters = lambdaSymbol.Parameters;
            if (!parameters.IsDefaultOrEmpty)
            {
                RecordDefinitions(parameters);
                foreach (var parameter in lambdaSymbol.Parameters)
                {
                    this.parameterMap.Add(parameter.Name, parameter);
                }
            }
        }

        private void RecordDefinitions(ImmutableArray<ParameterSymbol> definitions)
        {
            var declarationMap = this.definitionMap ?? (this.definitionMap = new SmallDictionary<string, ParameterSymbol>());
            foreach (var s in definitions)
            {
                if (!declarationMap.ContainsKey(s.Name))
                {
                    declarationMap.Add(s.Name, s);
                }
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

        private bool ReportConflictWithParameter(ParameterSymbol parameter, Symbol newSymbol, string name, Location newLocation, DiagnosticBag diagnostics)
        {
            if (parameter.Locations[0] == newLocation)
            {
                // a query variable and its corresponding lambda parameter, for example
                return false;
            }

            var oldLocation = parameter.Locations[0];

            Debug.Assert(oldLocation != newLocation || oldLocation == Location.None, "same nonempty location refers to different symbols?");

            SymbolKind parameterKind = parameter.Kind;
            // Quirk of the way we represent lambda parameters.                
            SymbolKind newSymbolKind = (object)newSymbol == null ? SymbolKind.Parameter : newSymbol.Kind;

            if (newSymbolKind == SymbolKind.ErrorType) return true;

            if (newSymbolKind == SymbolKind.Parameter || newSymbolKind == SymbolKind.Local)
            {
                // CS0412: 'X': a parameter or local variable cannot have the same name as a method type parameter
                diagnostics.Add(ErrorCode.ERR_LocalSameNameAsTypeParam, newLocation, name);
                return true;
            }

            if (newSymbolKind == SymbolKind.Parameter || newSymbolKind == SymbolKind.Local)
            {
                // A local or parameter named '{0}' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                diagnostics.Add(ErrorCode.ERR_LocalIllegallyOverrides, newLocation, name);
                return true;
            }

            if (newSymbolKind == SymbolKind.RangeVariable)
            {
                // The range variable '{0}' conflicts with a previous declaration of '{0}'
                diagnostics.Add(ErrorCode.ERR_QueryRangeVariableOverrides, newLocation, name);
                return true;
            }

            Debug.Assert(false, "what else could be defined in a lambda?");
            return false;
        }


        internal override bool EnsureSingleDefinition(Symbol symbol, string name, Location location, DiagnosticBag diagnostics)
        {
            ParameterSymbol existingDeclaration;
            var map = this.definitionMap;
            if (map != null && map.TryGetValue(name, out existingDeclaration))
            {
                return ReportConflictWithParameter(existingDeclaration, symbol, name, location, diagnostics);
            }

            return false;
        }
    }
}