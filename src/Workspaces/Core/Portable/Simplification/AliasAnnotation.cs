// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


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
        {
            return annotation.Data;
        }

        public static SyntaxAnnotation Create(string aliasName)
        {
            return new SyntaxAnnotation(Kind, aliasName);
        }
    }
}
