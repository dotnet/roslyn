// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    /// <summary>
    /// Indicate the relationship between the member and its inheritance target
    /// </summary>
    [Flags]
    internal enum InheritanceRelationship
    {
        /// <summary>
        /// A default case that should not be used.
        /// </summary>
        None = 0,

        // class & struct
        ImplementedInterface = 1,
        BaseType = 2,
        DerivedType = 4,

        // interface
        InheritedInterface = 8,
        ImplementingType = 16,

        // class & structure members
        ImplmentedMember = 32,
        OverriddenMember = 64,
        OverridingMember = 128,

        // member of interface
        ImplementingMember = 256
    }
}
