// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Note, the value is an array creation expression that encapsulates all the elements, if any.
        /// </summary>
        ParamArray = 0x2,

        /// <summary>
        /// Argument is a default value supplied automatically by the compilers.
        /// </summary>
        DefaultValue = 0x3,

        /// <summary>
        /// Argument is a param collection created by compilers for the matching C# params parameter. 
        /// Note, the value is a collection expression that encapsulates all the elements, if any.
        /// </summary>
        ParamCollection = 0x4,
    }
}

