﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.InheritanceMargin
{
    /// <summary>
    /// Indicate the relationship between the member and its inheritance target.
    /// Note: the value of the enum value is used to order headers of the context menu items, and to make sure they match
    /// the content of tooltip.
    /// </summary>
    [Flags]
    internal enum InheritanceRelationship
    {
        /// <summary>
        /// A default case that should not be used.
        /// </summary>
        None = 0,

        /// <summary>
        /// Implemented interfaces for class or struct. Shown as I↑
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
        /// Overriding member for member in class or structure. Shown as O↓
        /// </summary>
        OverridingMember = 128,

        /// <summary>
        /// Implementing member for member in interface. Shown as I↓
        /// </summary>
        ImplementingMember = 256,
    }
}
