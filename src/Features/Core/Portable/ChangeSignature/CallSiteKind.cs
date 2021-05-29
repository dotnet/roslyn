// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal enum CallSiteKind
    {
        /// <summary>
        /// Use an explicit value to populate call sites, without forcing
        /// the addition of a named argument.
        /// </summary>
        Value,

        /// <summary>
        /// Use an explicit value to populate call sites, and convert 
        /// arguments to named arguments even if not required. Often
        /// useful for literal callsite values like "true" or "null".
        /// </summary>
        ValueWithName,

        /// <summary>
        /// Indicates whether a "TODO" should be introduced at callsites
        /// to cause errors that the user can then go visit and fix up.
        /// </summary>
        Todo,

        /// <summary>
        /// When an optional parameter is added, passing an argument for
        /// it is not required. This indicates that the corresponding argument 
        /// should be omitted. This often results in subsequent arguments needing
        /// to become named arguments
        /// </summary>
        Omitted,

        /// <summary>
        /// Populate each call site with an available variable of a matching types.
        /// If no matching variable is found, this falls back to the 
        /// <see cref="Todo"/> behavior.
        /// </summary>
        Inferred
    }
}
