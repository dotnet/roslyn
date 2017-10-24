// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a collection element initializer.
    /// <para>
    /// Current usage:
    ///  (1) C# nested collection element initializer expression within a collection initializer.
    ///      For example, given a collection initializer "new Class() { Y = { { x, y, 3 } } }",
    ///      nested collection element initializer for Y, i.e. "{ { x, y, 3 } }" is represented by this operation.
    ///  (2) VB nested collection element initializer expression within a collection initializer.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ICollectionElementInitializerOperation : IOperation
    {
        /// <summary>
        /// Add method invoked on collection. Null for dynamic invocation and error cases.
        /// </summary>
        IMethodSymbol AddMethod { get; }

        /// <summary>
        /// Arguments passed to add method invocation.
        /// </summary>
        ImmutableArray<IOperation> Arguments { get; }

        /// <summary>
        /// Flag indicating if this is a dynamic invocation.
        /// </summary>
        bool IsDynamic { get; }
    }
}
