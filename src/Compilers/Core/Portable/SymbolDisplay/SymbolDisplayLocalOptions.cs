// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the options for how locals are displayed in the description of a symbol.
    /// </summary>
    [Flags]
    public enum SymbolDisplayLocalOptions
    {
        /// <summary>
        /// Shows only the name of the local.
        /// For example, "x".
        /// </summary>
        None = 0,

        /// <summary>
        /// Shows the type of the local in addition to its name.
        /// For example, "int x" in C# or "x As Integer" in Visual Basic.
        /// </summary>
        IncludeType = 1 << 0,

        /// <summary>
        /// Shows the constant value of the local, if there is one, in addition to its name.
        /// For example "x = 1".
        /// </summary>
        IncludeConstantValue = 1 << 1,

        /// <summary>
        /// Includes the <c>ref</c> keyword for ref-locals.
        /// </summary>
        IncludeRef = 1 << 2,
    }
}
