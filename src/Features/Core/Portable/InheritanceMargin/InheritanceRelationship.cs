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

        /// <summary>
        /// Implented interfaces for class or struct. Shown as I↑
        /// </summary>
        ImplementedInterface = 1,

        /// <summary>
        /// Base type for class or struct. Shown as O↑
        /// </summary>
        BaseType = 2,

        /// <summary>
        /// Derived type for class or struct. Shown as O↓
        /// </summary>
        DerivedType = 4,

        /// <summary>
        /// Inherited interface for interface. Shown as I↑
        /// </summary>
        InheritedInterface = 8,

        /// <summary>
        /// Implementing class, struct and interface for interface. Shown as I↓
        /// </summary>
        ImplementingType = 16,

        /// <summary>
        /// Implemented member for member in class or structure. Shown as I↑
        /// </summary>
        ImplementedMember = 32,

        /// <summary>
        /// Overridden member for member in class or structure. Shown as O↑
        /// </summary>
        OverriddenMember = 64,

        /// <summary>
        /// Overrrding member for member in class or structure. Shown as O↓
        /// </summary>
        OverridingMember = 128,

        /// <summary>
        /// Implmenting member for member in interface. Shown as I↓
        /// </summary>
        ImplementingMember = 256
    }
}
