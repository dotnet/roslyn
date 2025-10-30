// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis;

internal static class ExceptionExtensions
{
    /// <summary>
    /// Determine if an exception was an <see cref="OperationCanceledException"/>, and that the provided token caused the cancellation.
    /// </summary>
    /// <param name="exception">The exception to test.</param>
    /// <param name="cancellationToken">Checked to see if the provided token was cancelled.</param>
    /// <returns><see langword="true"/> if the exception was an <see cref="OperationCanceledException" /> and the token was canceled.</returns>
    internal static bool IsCurrentOperationBeingCancelled(this Exception exception, CancellationToken cancellationToken)
        => exception is OperationCanceledException && cancellationToken.IsCancellationRequested;
}
