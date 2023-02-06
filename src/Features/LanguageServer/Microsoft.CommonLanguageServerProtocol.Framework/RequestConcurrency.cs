// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CommonLanguageServerProtocol.Framework
{
    [Flags]
    public enum RequestConcurrency
    {
        /// <summary>
        /// Places no restraints on the concurrency of this request
        /// </summary>
        Parallel = 0,

        /// <summary>
        /// Indicates this request requires previous requests to be cancelled before execution
        /// </summary>
        /// <remarks>
        /// This was added for WebTools consumption as they aren't resilient to previous
        /// incomplete requests continuing execution during didChange notifications. As their
        /// parse trees are mutable, a didChange notification requires all previous requests
        /// to be completed before processing. This is similar to the O#
        /// WithContentModifiedSupport(false) behavior.
        /// </remarks>
        RequiresPreviousQueueItemsCancelled = 1,

        /// <summary>
        /// Indicates this request requires no new requests to be executed until it completes
        /// </summary>
        RequiresCompletionBeforeFurtherQueueing = 2,
    }
}
