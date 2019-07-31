// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an implicit/explicit reference to an instance.
    /// <para>
    /// Current usage:
    ///  (1) C# this or base expression.
    ///  (2) VB Me, MyClass, or MyBase expression.
    ///  (3) C# object or collection initializers.
    ///  (4) VB With statements, object or collection initializers.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInstanceReferenceOperation : IOperation
    {
        /// <summary>
        /// The kind of reference that is being made.
        /// </summary>
        InstanceReferenceKind ReferenceKind { get; }
    }
}
