// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.Interactive
{
    [Serializable]
    internal readonly struct RemoteExecutionResult
    {
        public readonly bool Success;

        /// <summary>
        /// New value of source search paths after execution, or null if not changed since the last execution.
        /// </summary>
        public readonly string[]? ChangedSourcePaths;

        /// <summary>
        /// New value of reference search paths after execution, or null if not changed since the last execution.
        /// </summary>
        public readonly string[]? ChangedReferencePaths;

        /// <summary>
        /// New value of working directory in the remote process after execution, or null if not changed since the last execution.
        /// </summary>
        public readonly string? ChangedWorkingDirectory;

        public RemoteExecutionResult(
            bool success,
            string[]? changedSourcePaths = null,
            string[]? changedReferencePaths = null,
            string? changedWorkingDirectory = null)
        {
            Success = success;
            ChangedSourcePaths = changedSourcePaths;
            ChangedReferencePaths = changedReferencePaths;
            ChangedWorkingDirectory = changedWorkingDirectory;
        }
    }
}
