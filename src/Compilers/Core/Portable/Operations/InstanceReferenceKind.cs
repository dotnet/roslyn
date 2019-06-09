// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Kind of reference for an <see cref="IInstanceReferenceOperation"/>.
    /// </summary>
    public enum InstanceReferenceKind
    {
        /// <summary>
        /// Reference to an instance of the containing type. Used for <code>this</code> and <code>base</code> in C# code, and <code>Me</code>,
        /// <code>MyClass</code>, <code>MyBase</code> in VB code.
        /// </summary>
        ContainingTypeInstance,
        /// <summary>
        /// Reference to the object being initialized in C# or VB object or collection initializer,
        /// anonymous type creation initializer, or to the object being referred to in a VB With statement.
        /// </summary>
        ImplicitReceiver,
        /// <summary>
        /// Reference to the value being matching in a property subpattern.
        /// </summary>
        PatternInput,
    }
}
