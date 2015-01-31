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
        public readonly string[] NewSourcePaths;

        /// <summary>
        /// New value of reference search paths after execution, or null if not changed since the last execution.
        /// </summary>
        public readonly string[] NewReferencePaths;

        /// <summary>
        /// New value of working directory in the remote process after execution, or null if not changed since the last execution.
        /// </summary>
        public readonly string NewWorkingDirectory;

        /// <summary>
        /// Resolved path if applicable. Used by ExecuteFile.
        /// </summary>
        public readonly string ResolvedPath;

        public RemoteExecutionResult(
            bool success,
            string[] newSourcePaths = null,
            string[] newReferencePaths = null,
            string newWorkingDirectory = null,
            string resolvedPath = null)
        {
            this.Success = success;
            this.NewSourcePaths = newSourcePaths;
            this.NewReferencePaths = newReferencePaths;
            this.NewWorkingDirectory = newWorkingDirectory;
            this.ResolvedPath = resolvedPath;
        }
    }
}
