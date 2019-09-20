// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class WithLambdaParametersBinder : LocalScopeBinder
    {
        protected readonly LambdaSymbol lambdaSymbol;
        protected readonly MultiDictionary<string, ParameterSymbol> parameterMap;
        private SmallDictionary<string, ParameterSymbol> _definitionMap;

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
                    if (!(parameter is IDiscardSymbol))
                    {
                        this.parameterMap.Add(parameter.Name, parameter);
                    }
                }
            }
        }

        private void RecordDefinitions(ImmutableArray<ParameterSymbol> definitions)
        {
            var declarationMap = _definitionMap ?? (_definitionMap = new SmallDictionary<string, ParameterSymbol>());
            foreach (var s in definitions)
            {
                if (!(s is IDiscardSymbol) && !declarationMap.ContainsKey(s.Name))
                {
                    declarationMap.Add(s.Name, s);
                }
            }
        }

        protected override TypeSymbol GetCurrentReturnType(out RefKind refKind)
        {
            refKind = lambdaSymbol.RefKind;
            return lambdaSymbol.ReturnType;
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get
            {
                return this.lambdaSymbol;
            }
        }

        internal override bool IsNestedFunctionBinder => true;

        internal override bool IsDirectlyInIterator
        {
            get
            {
                return false;
            }
        }

        // NOTE: Specifically not overriding IsIndirectlyInIterator.

        internal override TypeWithAnnotations GetIteratorElementType(YieldStatementSyntax node, DiagnosticBag diagnostics)
        {
            if (node != null)
            {
                diagnostics.Add(ErrorCode.ERR_YieldInAnonMeth, node.YieldKeyword.GetLocation());
            }
            return TypeWithAnnotations.Create(CreateErrorType());
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(result.IsClear);

            if ((options & LookupOptions.NamespaceAliasesOnly) != 0)
            {
                return;
            }

            foreach (var parameterSymbol in parameterMap[name])
            {
                result.MergeEqual(originalBinder.CheckViability(parameterSymbol, arity, options, null, diagnose, ref useSiteDiagnostics));
            }
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (options.CanConsiderMembers())
            {
                foreach (var parameter in lambdaSymbol.Parameters)
                {
                    if (originalBinder.CanAddLookupSymbolInfo(parameter, options, result, null))
                    {
                        result.AddSymbol(parameter, parameter.Name, 0);
                    }
                }
            }
        }

        private static bool ReportConflictWithParameter(ParameterSymbol parameter, Symbol newSymbol, string name, Location newLocation, DiagnosticBag diagnostics)
        {
            var oldLocation = parameter.Locations[0];
            if (oldLocation == newLocation)
            {
                // a query variable and its corresponding lambda parameter, for example
                return false;
            }

            // Quirk of the way we represent lambda parameters.                
            SymbolKind newSymbolKind = (object)newSymbol == null ? SymbolKind.Parameter : newSymbol.Kind;

            switch (newSymbolKind)
            {
                case SymbolKind.ErrorType:
                    return true;

                case SymbolKind.Parameter:
                case SymbolKind.Local:
                    // Error: A local or parameter named '{0}' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    diagnostics.Add(ErrorCode.ERR_LocalIllegallyOverrides, newLocation, name);
                    return true;

                case SymbolKind.Method:
                    // Local function declaration name conflicts are not reported, for backwards compatibility.
                    return false;

                case SymbolKind.TypeParameter:
                    // Type parameter declaration name conflicts are not reported, for backwards compatibility.
                    return false;

                case SymbolKind.RangeVariable:
                    // The range variable '{0}' conflicts with a previous declaration of '{0}'
                    diagnostics.Add(ErrorCode.ERR_QueryRangeVariableOverrides, newLocation, name);
                    return true;
            }

            Debug.Assert(false, "what else could be defined in a lambda?");
            diagnostics.Add(ErrorCode.ERR_InternalError, newLocation);
            return false;
        }

        internal override bool EnsureSingleDefinition(Symbol symbol, string name, Location location, DiagnosticBag diagnostics)
        {
            ParameterSymbol existingDeclaration;
            var map = _definitionMap;
            if (map != null && map.TryGetValue(name, out existingDeclaration))
            {
                return ReportConflictWithParameter(existingDeclaration, symbol, name, location, diagnostics);
            }

            return false;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override uint LocalScopeDepth => Binder.TopLevelScope;
    }
}
