// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    /// <summary>
    /// Annotation placed if the code generator encounters a NullableAttribute or NullableContextAttribute while
    /// generating the code.
    /// </summary>
    internal sealed class NullableSyntaxAnnotation
    {
        public static readonly SyntaxAnnotation None = new SyntaxAnnotation($"{nameof(NullableAnnotation)}.{NullableAnnotation.None}");
        public static readonly SyntaxAnnotation Annotated = new SyntaxAnnotation($"{nameof(NullableAnnotation)}.{NullableAnnotation.Annotated}");
        public static readonly SyntaxAnnotation NotAnnotated = new SyntaxAnnotation($"{nameof(NullableAnnotation)}.{NullableAnnotation.NotAnnotated}");
    }
}
