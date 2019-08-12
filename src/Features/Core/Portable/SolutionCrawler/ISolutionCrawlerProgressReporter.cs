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
        /// Raised when solution crawler progress changed
        /// 
        /// Notifications for this event are serialized to preserve order. 
        /// However, individual event notifications may occur on any thread.
        /// </summary>
        event EventHandler<ProgressData> ProgressChanged;
    }

    internal struct ProgressData
    {
        public ProgressStatus Status { get; }
        public string FilePathOpt { get; }

        public ProgressData(ProgressStatus type, string filePathOpt)
        {
            Status = type;
            FilePathOpt = filePathOpt;
        }
    }

    internal enum ProgressStatus
    {
        Started,
        Updated,
        Stoped
    }
}
