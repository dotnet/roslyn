// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Emit
{
    [Flags]
    public enum SemanticEditOption
    {
        /// <summary>
        /// Nothing special about this edit
        /// </summary>
        None = 0,

        /// <summary>
        /// The semantic edit is for a method update, and it requires emitting all parameters as part of the update
        /// </summary>
        EmitAllParametersForMethodUpdate
    }
}
