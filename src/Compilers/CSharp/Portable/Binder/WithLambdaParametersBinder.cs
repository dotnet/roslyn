// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
        private readonly SmallDictionary<string, ParameterSymbol> _definitionMap;

        public WithLambdaParametersBinder(LambdaSymbol lambdaSymbol, Binder enclosing)
            : base(enclosing)
        {
            this.lambdaSymbol = lambdaSymbol;
            this.parameterMap = new MultiDictionary<string, ParameterSymbol>();

            var parameters = lambdaSymbol.Parameters;
            if (!parameters.IsDefaultOrEmpty)
            {
                _definitionMap = new SmallDictionary<string, ParameterSymbol>();
                foreach (var parameter in parameters)
                {
                    if (!parameter.IsDiscard)
                    {
                        var name = parameter.Name;
                        this.parameterMap.Add(name, parameter);
                        if (!_definitionMap.ContainsKey(name))
                        {
                            _definitionMap.Add(name, parameter);
                        }
                    }
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

        internal override TypeWithAnnotations GetIteratorElementType()
        {
            return TypeWithAnnotations.Create(CreateErrorType());
        }

        protected override void ValidateYield(YieldStatementSyntax node, BindingDiagnosticBag diagnostics)
        {
            if (node != null)
            {
                diagnostics.Add(ErrorCode.ERR_YieldInAnonMeth, node.YieldKeyword.GetLocation());
            }
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(result.IsClear);

            if ((options & LookupOptions.NamespaceAliasesOnly) != 0)
            {
                return;
            }

            foreach (var parameterSymbol in parameterMap[name])
            {
                result.MergeEqual(originalBinder.CheckViability(parameterSymbol, arity, options, null, diagnose, ref useSiteInfo));
            }
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
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

        private static bool ReportConflictWithParameter(ParameterSymbol parameter, Symbol newSymbol, string name, Location newLocation, BindingDiagnosticBag diagnostics)
        {
            var oldLocation = parameter.GetFirstLocation();
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

        internal override bool EnsureSingleDefinition(Symbol symbol, string name, Location location, BindingDiagnosticBag diagnostics)
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
            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
