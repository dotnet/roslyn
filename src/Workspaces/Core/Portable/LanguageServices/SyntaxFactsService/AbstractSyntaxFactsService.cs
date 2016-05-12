using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractSyntaxFactsService
    {
        private readonly static ObjectPool<List<Dictionary<string, string>>> s_aliasMapListPool =
            new ObjectPool<List<Dictionary<string, string>>>(() => new List<Dictionary<string, string>>());
        private readonly static ObjectPool<Dictionary<string, string>> s_aliasMapPool =
            new ObjectPool<Dictionary<string, string>>(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        protected static List<Dictionary<string, string>> AllocateAliasMapList()
        {
            return s_aliasMapListPool.Allocate();
        }

        protected static void FreeAliasMapList(List<Dictionary<string, string>> list)
        {
            if (list != null)
            {
                foreach (var aliasMap in list)
                {
                    FreeAliasMap(aliasMap);
                }

                s_aliasMapListPool.ClearAndFree(list);
            }
        }

        protected static void FreeAliasMap(Dictionary<string, string> aliasMap)
        {
            if (aliasMap != null)
            {
                s_aliasMapPool.ClearAndFree(aliasMap);
            }
        }

        protected static Dictionary<string, string> AllocateAliasMap()
        {
            return s_aliasMapPool.Allocate();
        }
    }
}
