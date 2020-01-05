// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Kinds of arguments.
    /// </summary>
    public enum ArgumentKind
    {
        /// <summary>
        /// Represents unknown argument kind.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Argument value is explicitly supplied.
        /// </summary>
        Explicit = 0x1,

        /// <summary>
        /// Argument is a param array created by compilers for the matching C# params or VB ParamArray parameter. 
        /// Note, the value is a an array creation expression that encapsulates all the elements, if any.
        /// </summary>
        ParamArray = 0x2,

        /// <summary>
        /// Argument is a default value supplied automatically by the compilers.
        /// </summary>
        DefaultValue = 0x3
    }
}

