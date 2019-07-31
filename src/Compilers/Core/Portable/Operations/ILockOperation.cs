// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a <see cref="Body" /> of operations that are executed while holding a lock onto the <see cref="LockedValue" />.
    /// <para>
    /// Current usage:
    ///  (1) C# lock statement.
    ///  (2) VB SyncLock statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ILockOperation : IOperation
    {
        /// <summary>
        /// Operation producing a value to be locked.
        /// </summary>
        IOperation LockedValue { get; }
        /// <summary>
        /// Body of the lock, to be executed while holding the lock.
        /// </summary>
        IOperation Body { get; }
    }
}
