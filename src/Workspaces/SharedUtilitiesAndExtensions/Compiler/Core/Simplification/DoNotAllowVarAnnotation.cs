// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Simplification
{
    /// <summary>
    /// When applied to a SyntaxNode, prevents the simplifier from converting a type to 'var'.
    /// </summary>
    internal class DoNotAllowVarAnnotation
    {
        public static readonly SyntaxAnnotation Annotation = new(Kind);
        public const string Kind = "DoNotAllowVar";
    }
}
