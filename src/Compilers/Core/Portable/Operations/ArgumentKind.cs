// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Kinds of arguments.
    /// </summary>
    public enum ArgumentKind
    {
        None = 0x0,

        /// <summary>
        /// Argument is specified positionally and matches the parameter of the same ordinality.
        /// </summary>
        Positional = 0x1,
        /// <summary>
        /// Argument is specified by name and matches the parameter of the same name.
        /// </summary>
        Named = 0x2,
        /// <summary>
        /// Argument becomes an element of an array that matches a trailing C# params or VB ParamArray parameter.
        /// </summary>
        ParamArray = 0x3,
        /// <summary>
        /// Argument was omitted in source but has a default value supplied automatically.
        /// </summary>
        DefaultValue = 0x4
    }
}

