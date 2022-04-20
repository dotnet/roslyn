// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Notification
{
    internal interface IGlobalOperationRegistration : IDisposable
    {
        /// <summary>
        /// Used to indicate that the global operation completed fully.  The only effect this has is how this operation
        /// will be logged when <see cref="IDisposable.Dispose"/> is called. If this has been called, then <see
        /// cref="IDisposable.Dispose"/> will log that we completed without cancellation.  If this has not been called,
        /// then <see cref="IDisposable.Dispose"/> will log that we were canceled.
        /// </summary>
        void Done();
    }
}
