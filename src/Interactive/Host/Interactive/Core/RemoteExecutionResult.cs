// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal readonly struct RemoteExecutionResult
    {
        internal sealed class Data
        {
            public bool Success;
            public string[]? ChangedSourcePaths;
            public string[]? ChangedReferencePaths;
            public string? ChangedWorkingDirectory;
            public RemoteInitializationResult.Data? InitializationResult;

            public RemoteExecutionResult Deserialize()
                => new RemoteExecutionResult(
                    Success,
                    ChangedSourcePaths?.ToImmutableArray() ?? default,
                    ChangedReferencePaths?.ToImmutableArray() ?? default,
                    ChangedWorkingDirectory,
                    InitializationResult?.Deserialize());
        }

        public readonly bool Success;

        /// <summary>
        /// New value of source search paths after execution, or <see langword="default"/> if not changed since the last execution.
        /// </summary>
        public readonly ImmutableArray<string> ChangedSourcePaths;

        /// <summary>
        /// New value of reference search paths after execution, or <see langword="default"/> if not changed since the last execution.
        /// </summary>
        public readonly ImmutableArray<string> ChangedReferencePaths;

        /// <summary>
        /// New value of working directory in the remote process after execution, or null if not changed since the last execution.
        /// </summary>
        public readonly string? ChangedWorkingDirectory;

        public readonly RemoteInitializationResult? InitializationResult;

        public RemoteExecutionResult(
            bool success,
            ImmutableArray<string> changedSourcePaths = default,
            ImmutableArray<string> changedReferencePaths = default,
            string? changedWorkingDirectory = null,
            RemoteInitializationResult? initializationResult = null)
        {
            Success = success;
            ChangedSourcePaths = changedSourcePaths;
            ChangedReferencePaths = changedReferencePaths;
            ChangedWorkingDirectory = changedWorkingDirectory;
            InitializationResult = initializationResult;
        }

        public Data Serialize()
            => new Data()
            {
                Success = Success,
                ChangedSourcePaths = ChangedSourcePaths.IsDefault ? null : ChangedSourcePaths.ToArray(),
                ChangedReferencePaths = ChangedReferencePaths.IsDefault ? null : ChangedReferencePaths.ToArray(),
                ChangedWorkingDirectory = ChangedWorkingDirectory,
                InitializationResult = InitializationResult?.Serialize(),
            };
    }
}
