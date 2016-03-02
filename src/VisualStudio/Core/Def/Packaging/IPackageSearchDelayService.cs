// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    /// <summary>
    /// Used so we can mock out how the search service delays work for testing purposes.
    /// </summary>
    internal interface IPackageSearchDelayService
    {
        /// <summary>
        /// They time to wait after a successful update (default = 1 day).
        /// </summary>
        TimeSpan UpdateSucceededDelay { get; }

        /// <summary>
        /// They time to wait after a failed update (default = 1 minute).
        /// </summary>
        TimeSpan UpdateFailedDelay { get; }

        /// <summary>
        /// They time to wait after writing to disk fails (default = 10 seconds).
        /// </summary>
        TimeSpan FileWriteDelay { get; }

        /// <summary>
        /// How long to wait between each poll of the cache (default = 1 minute).
        /// </summary>
        TimeSpan CachePollDelay { get; }
    }
}