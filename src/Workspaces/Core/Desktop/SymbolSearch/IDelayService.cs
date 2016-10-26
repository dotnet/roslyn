﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    /// <summary>
    /// Used so we can mock out how the search service delays work for testing purposes.
    /// </summary>
    internal interface IDelayService
    {
        /// <summary>
        /// The time to wait after a successful update (default = 1 day).
        /// </summary>
        TimeSpan UpdateSucceededDelay { get; }

        /// <summary>
        /// The time to wait after a simple expected sort of failure (i.e. IO exceptions, 
        /// network exceptions, etc).  Things we can recover from and would expect would 
        /// be transient.
        /// </summary>
        TimeSpan ExpectedFailureDelay { get; }

        /// <summary>
        /// The time to wait after a catastrophic failed update (default = 1 day).  For
        /// example, if we download the full DB xml from the server and we cannot parse
        /// it.  Retrying soon after will not help.  We'll just have to wait until proper
        /// data is on the server for us to query.
        /// </summary>
        TimeSpan CatastrophicFailureDelay { get; }

        /// <summary>
        /// The time to wait after writing to disk fails (default = 10 seconds).
        /// </summary>
        TimeSpan FileWriteDelay { get; }

        /// <summary>
        /// How long to wait between each poll of the cache (default = 1 minute).
        /// </summary>
        TimeSpan CachePollDelay { get; }
    }
}
