using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Roslyn.Compilers.Internal;
using Roslyn.Compilers.MetadataReader;
using Roslyn.Compilers.Collections;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    internal class MergedGlobalBinderContext : BinderContext
    {
        internal MergedGlobalBinderContext(Compilation compilation)
            : base(null, null, compilation)
        {
        }

        internal override void Lookup(LookupResult result, string name, BinderContext original, LookupFilter filter)
        {
            Debug.Assert(result.IsClear());

            var results = Compilation.GlobalNamespace.GetMembers(name);
            original.FilterAccessibility(result, results, filter);
        }

        internal override void LookupType(LookupResult result, string name, int arity, BinderContext original, ConsList<Symbol> basesBeingResolved)
        {
            Debug.Assert(result.IsClear());

            original.FilterAccessibility(result, Compilation.GlobalNamespace.GetTypeMembers(name, arity));
        }

        internal override void LookupNamespaceOrType(LookupResult result, string name, BinderContext original, ConsList<Symbol> basesBeingResolved, LookupFilter filter)
        {
            Debug.Assert(result.IsClear());

            original.FilterAccessibility(result, Compilation.GlobalNamespace.GetMembers(name).OfType<Symbol, NamespaceOrTypeSymbol>(), filter);
        }
    }
}
