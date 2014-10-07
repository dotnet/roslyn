using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SyntaxNodeOrTokenListExtensions
    {
        /// <summary>
        /// Returns the index in <paramref name="list"/> for the given nodeOrToken.
        /// </summary>
        /// <param name="list">The list in which to search.</param>
        /// <param name="nodeOrToken">The node or token to search for in the list.</param>
        /// <returns>The index of the found nodeOrToken, or -1 if it wasn't found</returns>
        public static int IndexOf(this SyntaxNodeOrTokenList list, SyntaxNodeOrToken nodeOrToken)
        {
            var i = 0;
            foreach (var child in list)
            {
                if (child == nodeOrToken)
                {
                    return i;
                }

                i++;
            }

            return -1;
        }
    }
}
