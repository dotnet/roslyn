// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// The capabilities that the runtime has with respect to edit and continue
    /// </summary>
    [Flags]
    internal enum EditAndContinueCapabilities
    {
        None = 0,

        /// <summary>
        /// Edit and continue is generally available with the set of capabilities that Mono 6, .NET Framework and .NET 5 have in common.
        /// </summary>
        Baseline = 1 << 0,

        /// <summary>
        /// Adding a static or instance method to an existing type.
        /// </summary>
        AddMethodToExistingType = 1 << 1,

        /// <summary>
        /// Adding a static field to an existing type.
        /// </summary>
        AddStaticFieldToExistingType = 1 << 2,

        /// <summary>
        /// Adding an instance field to an existing type.
        /// </summary>
        AddInstanceFieldToExistingType = 1 << 3,

        /// <summary>
        /// Creating a new type definition.
        /// </summary>
        NewTypeDefinition = 1 << 4
    }
}
