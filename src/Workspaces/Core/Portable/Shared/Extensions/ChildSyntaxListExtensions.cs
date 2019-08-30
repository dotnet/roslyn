// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
