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
        ImplementedInterface = 1 << 0,

        /// <summary>
        /// Base type for class or struct. Shown as O↑
        /// </summary>
        BaseType = 1 << 1,

        /// <summary>
        /// Derived type for class or struct. Shown as O↓
        /// </summary>
        DerivedType = 1 << 2,

        /// <summary>
        /// Inherited interface for interface. Shown as I↑
        /// </summary>
        InheritedInterface = 1 << 3,

        /// <summary>
        /// Implementing class, struct and interface for interface. Shown as I↓
        /// </summary>
        ImplementingType = 1 << 4,

        /// <summary>
        /// Implemented member for member in class or structure. Shown as I↑
        /// </summary>
        ImplementedMember = 1 << 5,

        /// <summary>
        /// Overridden member for member in class or structure. Shown as O↑
        /// </summary>
        OverriddenMember = 1 << 6,

        /// <summary>
        /// Overrrding member for member in class or structure. Shown as O↓
        /// </summary>
        OverridingMember = 1 << 7,

        /// <summary>
        /// Implmenting member for member in interface. Shown as I↓
        /// </summary>
        ImplementingMember = 1 << 8,

        /// <summary>
        /// An import directive inherited from the global scope.
        /// </summary>
        InheritedImport = 1 << 9,
    }
}
