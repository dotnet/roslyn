// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Kind of reference for an <see cref="IInstanceReferenceOperation"/>.
    /// </summary>
    public enum InstanceReferenceKind
    {
        /// <summary>
        /// Reference to a member defined on the type of this instance reference, potentially overridden by the implementation.
        /// Used for C# <code>this</code> and VB <code>Me</code>.
        /// </summary>
        ThisOverridable,
        /// <summary>
        /// Reference to a member defined on the type of this instance reference, as if the member was defined with <code>NotOverridable</code>.
        /// Used for VB <code>MyClass</code>.
        /// </summary>
        ThisNonOverridable,
        /// <summary>
        /// Reference to a member defined on the base type of this instance reference.
        /// Used for C# <code>base</code> and VB <code>MyBase</code>.
        /// </summary>
        Base,
        /// <summary>
        /// Reference to a member of the object being initialized in a C# object or collection initializer, or a VB With statement.
        /// </summary>
        Initializer
    }
}
