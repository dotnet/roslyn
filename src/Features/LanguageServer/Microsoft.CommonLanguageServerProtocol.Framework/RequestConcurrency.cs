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
        RequiresPreviousQueueItemsCancelled = 1,

        /// <summary>
        /// Indicates this request requires no new requests to be executed until it completes
        /// </summary>
        RequiresCompletionBeforeFurtherQueueing = 2,
    }
}
