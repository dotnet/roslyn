// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Code fix category for code fixes provided by a <see cref="CodeFixProvider"/>.
    /// </summary>
    internal enum CodeFixCategory
    {
        /// <summary>
        /// Fixes code to adhere to code style.
        /// </summary>
        CodeStyle,

        /// <summary>
        /// Fixes code to improve code quality.
        /// </summary>
        CodeQuality,

        /// <summary>
        /// Fixes code to fix compiler diagnostics.
        /// </summary>
        Compile,

        /// <summary>
        /// Custom category for fix.
        /// </summary>
        Custom
    }
}
