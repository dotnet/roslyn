// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    public enum GeneratedKind
    {
        /// <summary>
        /// It is unknown if the <see cref="SyntaxTree"/> is automatically generated.
        /// </summary>
        Unknown,
        /// <summary>
        /// The <see cref="SyntaxTree"/> is not automatically generated.
        /// </summary>
        NotGenerated,
        /// <summary>
        /// The <see cref="SyntaxTree"/> is marked as automatically generated.
        /// </summary>
        MarkedGenerated
    }
}
