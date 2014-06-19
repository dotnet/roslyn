using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class SequenceExtensions
    {
        ///<summary>
        /// Takes a sequence and a predicate, and counts hits and misses. If the sequence
        /// has hitThreshold hits, returns true, otherwise, returns false. Gives up
        /// if there are missThreshold misses.
        /// </summary>
        public static bool AtLeast<T>(this IEnumerable<T> sequence, int hitThreshold, int missThreshold, Func<T, bool> predicate)
        {
            int hits = 0;
            int misses = 0;
            foreach (T item in sequence)
            {
                if (predicate(item))
                {
                    hits += 1;
                }
                else
                {
                    misses += 1;
                }

                if (hits >= hitThreshold)
                {
                    return true;
                }

                if (misses >= missThreshold)
                {
                    return false;
                }
            }
            return false;
        }
    }
}
