using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SymbolInfoExtensions
    {
        public static IEnumerable<Symbol> GetAllSymbols(this SymbolInfo info)
        {
            return GetAllSymbolsWorker(info).Distinct();
        }

        private static IEnumerable<Symbol> GetAllSymbolsWorker(this SymbolInfo info)
        {
            if (info.Symbol != null)
            {
                yield return info.Symbol;
            }

            foreach (var symbol in info.CandidateSymbols)
            {
                yield return symbol;
            }
        }

        public static Symbol GetAnySymbol(this SymbolInfo info)
        {
            return info.GetAllSymbols().FirstOrDefault();
        }

        public static Symbol GetAnySymbol(this SymbolInfo info, params CandidateReason[] allowableReasons)
        {
            if (info.Symbol != null)
            {
                return info.Symbol;
            }

            if (allowableReasons.Contains(info.CandidateReason))
            {
                return info.CandidateSymbols.FirstOrDefault();
            }

            return null;
        }

        public static IEnumerable<Symbol> GetBestOrAllSymbols(this SymbolInfo info)
        {
            if (info.Symbol != null)
            {
                return SpecializedCollections.SingletonEnumerable(info.Symbol);
            }
            else if (info.CandidateSymbols.Length > 0)
            {
                return info.CandidateSymbols;
            }

            return SpecializedCollections.EmptyEnumerable<Symbol>();
        }
    }
}
