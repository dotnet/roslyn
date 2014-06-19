using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class WithPrimaryConstructorParametersBinder : LocalScopeBinder
    {
        private readonly MethodSymbol primaryCtor;
        private readonly SmallDictionary<string, ParameterSymbol> definitionMap;
        private readonly MultiDictionary<string, ParameterSymbol> parameterMap;

        internal WithPrimaryConstructorParametersBinder(MethodSymbol primaryCtor, Binder next)
            : base(next)
        {
            Debug.Assert((object)primaryCtor != null);
            this.primaryCtor = primaryCtor;
            var parameters = primaryCtor.Parameters;

            var definitionMap = new SmallDictionary<string, ParameterSymbol>();

            for (int i = parameters.Length - 1; i >= 0; i--)
            {
                var parameter = parameters[i];
                definitionMap[parameter.Name] = parameter;
            }

            this.definitionMap = definitionMap;

            var parameterMap = new MultiDictionary<string, ParameterSymbol>(parameters.Length, EqualityComparer<string>.Default);

            foreach (var parameter in parameters)
            {
                parameterMap.Add(parameter.Name, parameter);
            }

            this.parameterMap = parameterMap;
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (options.CanConsiderLocals())
            {
                foreach (var parameter in primaryCtor.Parameters)
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

            var count = parameterMap.GetCountForKey(name);
            if (count == 1)
            {
                ParameterSymbol p;
                parameterMap.TryGetSingleValue(name, out p);
                result.MergeEqual(originalBinder.CheckViability(p, arity, options, null, diagnose, ref useSiteDiagnostics));
            }
            else if (count > 1)
            {
                var parameters = parameterMap[name];
                foreach (var sym in parameters)
                {
                    result.MergeEqual(originalBinder.CheckViability(sym, arity, options, null, diagnose, ref useSiteDiagnostics));
                }
            }
        }

        internal override bool EnsureSingleDefinition(Symbol symbol, string name, Location location, DiagnosticBag diagnostics)
        {
            ParameterSymbol existingDeclaration;
            var map = this.definitionMap;
            if (map != null && map.TryGetValue(name, out existingDeclaration))
            {
                return InMethodBinder.ReportConflictWithParameter(existingDeclaration, symbol, name, location, diagnostics);
            }

            return false;
        }
    }
}