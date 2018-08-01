// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal readonly struct InteractiveExecutionResult
    {
        public readonly bool Success;

        /// <summary>
        /// New value of source search paths after execution, or null if not changed since the last execution.
        /// </summary>
        public readonly ImmutableArray<string> ChangedSourcePaths;

        /// <summary>
        /// New value of reference search paths after execution, or null if not changed since the last execution.
        /// </summary>
        public readonly ImmutableArray<string> ChangedReferencePaths;

        /// <summary>
        /// New value of working directory in the remote process after execution, or null if not changed since the last execution.
        /// </summary>
        public readonly string ChangedWorkingDirectory;

        public InteractiveExecutionResult(
            bool success,
            ImmutableArray<string> changedSourcePaths = default,
            ImmutableArray<string> changedReferencePaths = default,
            string changedWorkingDirectory = null)
        {
            Success = success;
            ChangedSourcePaths = changedSourcePaths;
            ChangedReferencePaths = changedReferencePaths;
            ChangedWorkingDirectory = changedWorkingDirectory;
        }
    }
}
