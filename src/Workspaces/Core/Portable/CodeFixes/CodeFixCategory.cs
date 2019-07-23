// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
