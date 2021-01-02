// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Utilities
{
    /// <summary>
    /// Represents a single scope of a context of executing potentially long running operation. Scopes allow multiple
    /// components running within an operation to share the same context.
    /// </summary>
    internal interface IOperationScope : IDisposable
    {
        /// <summary>
        /// Gets user readable operation description.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Progress tracker instance to report operation progress.
        /// </summary>
        IProgress<ProgressInfo> Progress { get; }
    }
}
