// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    /// <summary>
    /// Provide a way to see whether solution crawler is started or not
    /// </summary>
    internal interface ISolutionCrawlerProgressReporter
    {
        /// <summary>
        /// Return true if solution crawler is in progress.
        /// </summary>
        bool InProgress { get; }

        /// <summary>
        /// Raised when there is pending work in solution crawler.
        /// </summary>
        event EventHandler Started;

        /// <summary>
        /// Raised when there is no more pending work in solution crawler.
        /// </summary>
        event EventHandler Stopped;
    }
}
