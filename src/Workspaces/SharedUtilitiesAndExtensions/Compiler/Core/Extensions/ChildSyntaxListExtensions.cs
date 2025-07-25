// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class ChildSyntaxListExtensions
{
    extension(ChildSyntaxList childSyntaxList)
    {
        public SyntaxNodeOrToken First(Func<SyntaxNodeOrToken, bool> predicate)
        {
            foreach (var syntaxNodeOrToken in childSyntaxList)
            {
                if (predicate(syntaxNodeOrToken))
                    return syntaxNodeOrToken;
            }

            // Delegate to Enumerable.Last which will throw the exception.
            return Enumerable.First(childSyntaxList, predicate);
        }

        public SyntaxNodeOrToken Last(Func<SyntaxNodeOrToken, bool> predicate)
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
