// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class UsingDirectiveSyntaxExtensions
    {
        public static void SortUsingDirectives(
            this List<UsingDirectiveSyntax> usingDirectives,
            SyntaxList<UsingDirectiveSyntax> existingDirectives,
            bool placeSystemNamespaceFirst)
        {
            var systemFirstInstance = UsingsAndExternAliasesDirectiveComparer.SystemFirstInstance;
            var normalInstance = UsingsAndExternAliasesDirectiveComparer.NormalInstance;

            var specialCaseSystem = placeSystemNamespaceFirst;
            var comparers = specialCaseSystem
                ? (systemFirstInstance, normalInstance)
                : (normalInstance, systemFirstInstance);

            // First, see if the usings were sorted according to the user's preference.  If so,
            // keep the same sorting after we add the using.  However, if the usings weren't sorted
            // according to their preference, then see if they're sorted in the other way.  If so
            // preserve that sorting as well.  That way if the user is working with a file that 
            // was written on a machine with a different default, the usings will stay in a 
            // reasonable order.
            if (existingDirectives.IsSorted(comparers.Item1))
            {
                usingDirectives.Sort(comparers.Item1);
            }
            else if (existingDirectives.IsSorted(comparers.Item2))
            {
                usingDirectives.Sort(comparers.Item2);
            }
        }
    }
}
