// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a <see cref="Body"/> of operations that are executed while using disposable <see cref="Resources"/>.
    /// <para>
    /// Current usage:
    ///  (1) C# using statement.
    ///  (2) VB Using statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IUsingOperation : IOperation
    {
        /// <summary>
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        IOperation Body { get; }

        /// <summary>
        /// Declaration introduced or resource held by the using.
        /// </summary>
        IOperation Resources { get; }

        /// <summary>
        /// Locals declared within the <see cref="Resources"/> with scope spanning across this entire <see cref="IUsingOperation"/>.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }
}

