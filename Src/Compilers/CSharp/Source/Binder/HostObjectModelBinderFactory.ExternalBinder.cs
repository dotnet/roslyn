using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    partial class HostObjectModelBinderFactory
    {
        private sealed class ExternalBinder : Binder
        {
            private readonly CompilationChain chain;

            public ExternalBinder(CompilationChain chain, Binder next)
                : base(next)
            {
                this.chain = chain;
            }

            protected override void LookupSymbolsInSingleBinder(LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, bool diagnose)
            {
                // TODO
            }

            protected override void LookupAritiesInSingleBinder(HashSet<int> result, string name)
            {
                // TODO
            }

            protected override void LookupSymbolNamesInSingleBinder(HashSet<string> result, LookupOptions options = LookupOptions.Default)
            {
                // TODO
            }
        }
    }
}