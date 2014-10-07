using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.Common;
using Roslyn.Services.Shared.Collections;
using Roslyn.Utilities;

namespace Roslyn.Services.FindReferences
{
    internal class FindReferencesResult : IEnumerable<ReferencedSymbol>
    {
        private readonly ConcurrentDictionary<ISymbol, ConcurrentSet<CommonLocation>> result =
            new ConcurrentDictionary<ISymbol, ConcurrentSet<CommonLocation>>();

        public IEnumerable<ISymbol> Definitions
        {
            get
            {
                return SpecializedCollections.ReadOnlyEnumerable(result.Keys);
            }
        }

        public IEnumerable<CommonLocation> GetReferences(ISymbol definition)
        {
            return SpecializedCollections.ReadOnlyEnumerable(GetOrCreateReferences(definition));
        }

        private ConcurrentSet<CommonLocation> GetOrCreateReferences(ISymbol definition)
        {
            Contract.Requires(definition.Locations.All(loc => loc != null));
            return result.GetOrAdd(definition, _ => new ConcurrentSet<CommonLocation>());
        }

        internal void AddDefinition(ISymbol symbol)
        {
            GetOrCreateReferences(symbol);
        }

        internal void AddReference(ISymbol definition, CommonLocation reference)
        {
            GetOrCreateReferences(definition).Add(reference);
        }

        public IEnumerator<ReferencedSymbol> GetEnumerator()
        {
            return result.Select(kvp => new ReferencedSymbol(kvp.Key, kvp.Value)).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}