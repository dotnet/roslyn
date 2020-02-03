// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Simplification
{
    public static partial class Simplifier
    {
        /// <summary>
        /// The annotation the reducer uses to identify sub trees to be reduced.
        /// The Expand operations add this annotation to nodes so that the Reduce operations later find them.
        /// </summary>
        public static SyntaxAnnotation Annotation { get; } = new SyntaxAnnotation();
    }
}
