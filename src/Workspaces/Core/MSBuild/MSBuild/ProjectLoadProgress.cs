// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Provides details as a project is loaded.
    /// </summary>
    public struct ProjectLoadProgress
    {
        /// <summary>
        /// The project for which progress is being reported.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// The operation that has just completed.
        /// </summary>
        public ProjectLoadOperation Operation { get; }

        /// <summary>
        /// The amount of time elapsed for this operation.
        /// </summary>
        public TimeSpan ElapsedTime { get; }

        internal ProjectLoadProgress(string filePath, ProjectLoadOperation operation, TimeSpan elapsedTime)
        {
            FilePath = filePath;
            Operation = operation;
            ElapsedTime = elapsedTime;
        }
    }
}
