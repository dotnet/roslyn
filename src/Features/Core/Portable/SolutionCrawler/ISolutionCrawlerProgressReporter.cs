// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Raised when solution crawler progress changed
        /// 
        /// Notifications for this event are serialized to preserve order. 
        /// However, individual event notifications may occur on any thread.
        /// </summary>
        event EventHandler<ProgressData> ProgressChanged;
    }

    internal readonly struct ProgressData(ProgressStatus type, int? pendingItemCount)
    {
        public ProgressStatus Status { get; } = type;

        /// <summary>
        /// number of pending work item in the queue. 
        /// null means N/A for the associated <see cref="Status"/>
        /// </summary>
        public int? PendingItemCount { get; } = pendingItemCount;
    }

    internal enum ProgressStatus
    {
        Started,
        Paused,
        PendingItemCountUpdated,
        Evaluating,
        Stopped
    }
}
