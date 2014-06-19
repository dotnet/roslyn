using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Roslyn.Compilers.CodeGen;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;

namespace Roslyn.Compilers.Common
{
    public partial class CommonCompilation
    {
        /// <summary>
        /// This is an implementation of a special symbol comparer, which is supposed to be used 
        /// for sorting original definition symbols (explicitly or explicitly declared in source 
        /// within the same container) in lexical order of their declarations. It will not work on 
        /// anything that uses non-source locations.
        /// </summary>
        private class LexicalOrderSymbolComparerImpl : IComparer<ISymbol>
        {
            private readonly CommonCompilation compilation;

            public LexicalOrderSymbolComparerImpl(CommonCompilation compilation)
            {
                this.compilation = compilation;
            }

            public int Compare(ISymbol x, ISymbol y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                var xLocations = x.Locations;
                var yLocations = y.Locations;

                if (xLocations.IsNullOrEmpty)
                {
                    if (!yLocations.IsNullOrEmpty)
                    {
                        return 1;
                    }
                }
                else if (yLocations.IsNullOrEmpty)
                {
                    return -1;
                }

                var xFirst = compilation.FirstSourceLocation(xLocations);
                var yFirst = compilation.FirstSourceLocation(yLocations);

                var diff = compilation.CompareSourceLocations(xFirst, yFirst);
                if (diff != 0)
                {
                    return diff;
                }

                diff = x.Kind.ToSortOrder() - y.Kind.ToSortOrder();
                if (diff != 0)
                {
                    return diff;
                }

                diff = String.Compare(x.Name, y.Name);
                Debug.Assert(diff != 0);
                return diff;
            }
        }
    }
}