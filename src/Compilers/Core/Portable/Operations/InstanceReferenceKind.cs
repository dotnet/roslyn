// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// anonymous type creation initializer, or to the object being referred to in a VB With statement,
        /// or the C# 'with' expression initializer.
        /// </summary>
        ImplicitReceiver,
        /// <summary>
        /// Reference to the value being matching in a property subpattern.
        /// </summary>
        PatternInput,
        /// <summary>
        /// Reference to the interpolated string handler instance created as part of a parent interpolated string handler conversion.
        /// </summary>
        InterpolatedStringHandler,
    }
}
