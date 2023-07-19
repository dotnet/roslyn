// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents the execution and presentation of a potentially long running operation with possible wait indication
/// providing cancellability, progress and information to the host.
/// </summary>
public interface ILongRunningOperation : IDisposable
{
    /// <summary>
    /// The current scope the progress operation is in.  Can be used to update the current <see
    /// cref="ILongRunningOperationScope.Description"/> or <see cref="ILongRunningOperationScope.Progress"/> of that
    /// scope.  If a nested scope is desired, it can be obtained with <see cref="AddScope"/>.
    /// </summary>
    /// <remarks>
    /// Only the caller of <see cref="AddScope"/> can dispose the returned scope.  Trying to dispose the scope returned
    /// from <see cref="CurrentScope"/> will throw an exception
    /// </remarks>
    ILongRunningOperationScope CurrentScope { get; }

    /// <summary>
    /// Adds a nested operation scope with its own description and progress tracker. The scope is removed from the
    /// context on dispose. A nested scope is valuable when an operation wants to add more detailed information,
    /// including independent progress for that portion of the operation.  When completions (<see
    /// cref="IDisposable.Dispose"/>), the description will be returned to the parent scope.
    /// </summary>
    /// <remarks>
    /// Only the caller of <see cref="AddScope"/> can dispose the returned scope.  Trying to dispose the scope returned
    /// from <see cref="CurrentScope"/> will throw an exception.
    /// </remarks>
    ILongRunningOperationScope AddScope(string description);
}
