// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    /// <summary>
    /// Specifies the desired placement of using directives.
    /// </summary>
    internal enum UsingDirectivesPlacement
    {
        /// <summary>
        /// Allow using directives inside or outside the namespace definition.
        /// </summary>
        Preserve,

        /// <summary>
        /// Place using directives inside the namespace definition.
        /// </summary>
        InsideNamespace,

        /// <summary>
        /// Place using directives outside the namespace definition.
        /// </summary>
        OutsideNamespace
    }
}
