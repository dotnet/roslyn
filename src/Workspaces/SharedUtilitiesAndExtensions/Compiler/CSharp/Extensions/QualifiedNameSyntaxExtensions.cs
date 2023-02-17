// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class QualifiedNameSyntaxExtensions
    {
        public static NameSyntax? GetLeftmostName(this QualifiedNameSyntax qualifiedName)
        {
            var result = qualifiedName.Left;

            while (result is QualifiedNameSyntax q)
                result = q.Left;

            return result;
        }
    }
}
