// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ChildSyntaxListExtensions
    {
        public static SyntaxNodeOrToken First(this ChildSyntaxList childSyntaxList, Func<SyntaxNodeOrToken, bool> predicate)
        {
            foreach (var syntaxNodeOrToken in childSyntaxList)
            {
                if (predicate(syntaxNodeOrToken))
                    return syntaxNodeOrToken;
            }

            // Delegate to Enumerable.Last which will throw the exception.
            return Enumerable.First(childSyntaxList, predicate);
        }

        public static SyntaxNodeOrToken Last(this ChildSyntaxList childSyntaxList, Func<SyntaxNodeOrToken, bool> predicate)
        {
            foreach (var syntaxNodeOrToken in childSyntaxList.Reverse())
            {
                if (predicate(syntaxNodeOrToken))
                    return syntaxNodeOrToken;
            }

            // Delegate to Enumerable.Last which will throw the exception.
            return Enumerable.Last(childSyntaxList, predicate);
        }
    }
}
