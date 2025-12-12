// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal static class ExceptionUtilities
    {
        /// <summary>
        /// Creates an <see cref="InvalidOperationException"/> with information about an unexpected value.
        /// </summary>
        /// <param name="o">The unexpected value.</param>
        /// <returns>The <see cref="InvalidOperationException"/>, which should be thrown by the caller.</returns>
        internal static Exception UnexpectedValue(object? o)
        {
            string output = string.Format("Unexpected value '{0}' of type '{1}'", o, (o != null) ? o.GetType().FullName : "<unknown>");
            Debug.Assert(false, output);

            // We do not throw from here because we don't want all Watson reports to be bucketed to this call.
            return new InvalidOperationException(output);
        }

        internal static Exception Unreachable([CallerFilePath] string? path = null, [CallerLineNumber] int line = 0)
            => new InvalidOperationException($"This program location is thought to be unreachable. File='{path}' Line={line}");

        /// <summary>
        /// Determine if an exception was an <see cref="OperationCanceledException"/>, and that the provided token caused the cancellation.
        /// </summary>
        /// <param name="exception">The exception to test.</param>
        /// <param name="cancellationToken">Checked to see if the provided token was cancelled.</param>
        /// <returns><see langword="true"/> if the exception was an <see cref="OperationCanceledException" /> and the token was canceled.</returns>
        internal static bool IsCurrentOperationBeingCancelled(Exception exception, CancellationToken cancellationToken)
            => exception is OperationCanceledException && cancellationToken.IsCancellationRequested;
    }
}
