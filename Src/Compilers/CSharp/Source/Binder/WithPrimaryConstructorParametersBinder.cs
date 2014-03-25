using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class WithPrimaryConstructorParametersBinder : Binder
    {
        private readonly MethodSymbol primaryCtor;
        private readonly bool shadowBackingFields;

        internal WithPrimaryConstructorParametersBinder(MethodSymbol primaryCtor, bool shadowBackingFields, Binder next)
            : base(next)
        {
            Debug.Assert((object)primaryCtor != null);
            this.primaryCtor = primaryCtor;
            this.shadowBackingFields = shadowBackingFields;
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (options.CanConsiderLocals())
            {
                foreach (var parameter in primaryCtor.Parameters)
                {
                    if ((shadowBackingFields || (object)parameter.PrimaryConstructorParameterBackingField == null) &&
                        originalBinder.CanAddLookupSymbolInfo(parameter, options, null))
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

            foreach (ParameterSymbol parameter in primaryCtor.Parameters)
            {
                if (parameter.Name == name)
                {
                    if (shadowBackingFields || (object)parameter.PrimaryConstructorParameterBackingField == null)
                    {
                        result.MergeEqual(originalBinder.CheckViability(parameter, arity, options, null, diagnose, ref useSiteDiagnostics));
                    }
                }
            }
        }
    }
}