// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Interactive
{
    [Serializable]
    internal struct RemoteExecutionResult
    {
        public readonly bool Success;

        /// <summary>
        /// New value of source search paths after execution, or null if not changed since the last execution.
        /// </summary>
        public readonly string[] ChangedSourcePaths;

        /// <summary>
        /// New value of reference search paths after execution, or null if not changed since the last execution.
        /// </summary>
        public readonly string[] ChangedReferencePaths;

        /// <summary>
        /// New value of working directory in the remote process after execution, or null if not changed since the last execution.
        /// </summary>
        public readonly string ChangedWorkingDirectory;

        public RemoteExecutionResult(
            bool success,
            string[] changedSourcePaths = null,
            string[] changedReferencePaths = null,
            string changedWorkingDirectory = null)
        {
            this.Success = success;
            this.ChangedSourcePaths = changedSourcePaths;
            this.ChangedReferencePaths = changedReferencePaths;
            this.ChangedWorkingDirectory = changedWorkingDirectory;
        }
    }
}
