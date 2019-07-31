// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a creation of anonymous object.
    /// <para>
    /// Current usage:
    ///  (1) C# "new { ... }" expression
    ///  (2) VB "New With { ... }" expression
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IAnonymousObjectCreationOperation : IOperation
    {
        /// <summary>
        /// Property initializers.
        /// Each initializer is an <see cref="ISimpleAssignmentOperation" />, with an <see cref="IPropertyReferenceOperation" />
        /// as the target whose Instance is an <see cref="IInstanceReferenceOperation" /> with <see cref="InstanceReferenceKind.ImplicitReceiver" /> kind.
        /// </summary>
        ImmutableArray<IOperation> Initializers { get; }
    }
}
