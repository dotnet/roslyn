using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal class AssemblySymbolMap
    {
        private readonly Dictionary<string, List<SymbolId>> nameToSymbolMap;
        private readonly Dictionary<string, List<SymbolId>> nameToSymbolMapIgnoreCase;

        private AssemblySymbolMap(
            Dictionary<string, List<SymbolId>> nameToSymbolMap,
            Dictionary<string, List<SymbolId>> nameToSymbolMapIgnoreCase)
        {
            this.nameToSymbolMap = nameToSymbolMap;
            this.nameToSymbolMapIgnoreCase = nameToSymbolMapIgnoreCase;
        }

        public IEnumerable<SymbolId> GetSymbolIds(string name, bool ignoreCase)
        {
            List<SymbolId> ids;
            if ((ignoreCase && this.nameToSymbolMapIgnoreCase.TryGetValue(name, out ids)) ||
                this.nameToSymbolMap.TryGetValue(name, out ids))
            {
                return ids;
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<SymbolId>();
            }
        }

        /// <summary>
        /// Get the symbol map for the assembly.
        /// This is done with async pattern so multiple tasks don't block or duplicate the same work to construct the map.
        /// </summary>
        public static async Task<AssemblySymbolMap> FromAsync(IAssemblySymbol assembly, CancellationToken cancellationToken)
        {
            AsyncLazy<AssemblySymbolMap> assemblyMap;
            if (!assemblyNameMap.TryGetValue(assembly, out assemblyMap))
            {
                assemblyMap = assemblyNameMap.GetValue(assembly, _a => new AsyncLazy<AssemblySymbolMap>(c => CreateAssemblySymbolMap(_a, c), cacheResult: true));
            }

            return await assemblyMap.GetValueAsync(cancellationToken).ConfigureAwait(false);
        }

        private static Task<AssemblySymbolMap> CreateAssemblySymbolMap(IAssemblySymbol assembly, CancellationToken cancellationToken)
        {
            var map = new Dictionary<string, List<SymbolId>>();
            var mapIgnoreCase = new Dictionary<string, List<SymbolId>>(StringComparer.InvariantCultureIgnoreCase);
            ComputeSymbolMap(assembly.GlobalNamespace, map, mapIgnoreCase);
            return Task.FromResult(new AssemblySymbolMap(map, mapIgnoreCase));
        }

        private static ConditionalWeakTable<IAssemblySymbol, AsyncLazy<AssemblySymbolMap>>
            assemblyNameMap = new ConditionalWeakTable<IAssemblySymbol, AsyncLazy<AssemblySymbolMap>>();

        private static void ComputeSymbolMap(
            INamespaceSymbol containingNamespace, 
            Dictionary<string, List<SymbolId>> map,
            Dictionary<string, List<SymbolId>> mapIgnoreCase)
        {
            foreach (var symbol in containingNamespace.GetMembers())
            {
                // add this namespace or type symbol if it can be referenced by name
                if (symbol.CanBeReferencedByName)
                {
                    var id = symbol.GetSymbolId();
                    AddSymbol(map, symbol.Name, id);
                    AddSymbol(mapIgnoreCase, symbol.Name, id);
                }

                // if it is a namespace, get all nested symbols too.
                var ns = symbol as INamespaceSymbol;
                if (ns != null)
                {
                    ComputeSymbolMap(ns, map, mapIgnoreCase);
                }

                // if it has extension methods, get them too.
                var ts = symbol as INamedTypeSymbol;
                if (ts != null)
                {
                    // get nested types
                    ComputeSymbolMap(ts, map, mapIgnoreCase);

                    if (ts.MightContainExtensionMethods)
                    {
                        foreach (var member in ts.GetMembers())
                        {
                            var method = member as IMethodSymbol;
                            if (method != null && method.IsExtensionMethod)
                            {
                                var id = method.GetSymbolId();
                                AddSymbol(map, method.Name, id);
                                AddSymbol(mapIgnoreCase, method.Name, id);
                            }
                        }
                    }
                }
            }
        }

        private static void ComputeSymbolMap(
            INamedTypeSymbol containingType,
            Dictionary<string, List<SymbolId>> map,
            Dictionary<string, List<SymbolId>> mapIgnoreCase)
        {
            foreach (var type in containingType.GetTypeMembers())
            {
                var id = type.GetSymbolId();
                AddSymbol(map, type.Name, id);
                AddSymbol(mapIgnoreCase, type.Name, id);

                ComputeSymbolMap(type, map, mapIgnoreCase);
            }
        }

        private static void AddSymbol(Dictionary<string, List<SymbolId>> map, string name, SymbolId id)
        {
            List<SymbolId> symbols;
            if (!map.TryGetValue(name, out symbols))
            {
                symbols = new List<SymbolId>();
                map.Add(name, symbols);
            }

            symbols.Add(id);
        }
    }
}