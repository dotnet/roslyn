// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# lock or a VB SyncLock statement.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ILockStatement : IOperation
    {
        /// <summary>
        /// Expression producing a value to be locked.
        /// </summary>
        IOperation Expression { get; }
        /// <summary>
        /// Body of the lock, to be executed while holding the lock.
        /// </summary>
        IOperation Body { get; }
    }
}

