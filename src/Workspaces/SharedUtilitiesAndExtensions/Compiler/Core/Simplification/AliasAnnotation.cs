// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Simplification
{
    /// <summary>
    /// This annotation will be used by the expansion/reduction to annotate expanded syntax nodes to store the information that an 
    /// alias was used before expansion.
    /// </summary>
    internal static class AliasAnnotation
    {
        public const string Kind = "Alias";

        public static string GetAliasName(SyntaxAnnotation annotation)
            => annotation.Data!;

        public static SyntaxAnnotation Create(string aliasName)
            => new(Kind, aliasName);
    }
}
