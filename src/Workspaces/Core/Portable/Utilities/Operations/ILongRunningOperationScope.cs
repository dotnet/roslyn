// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents a single scope of a context of executing potentially long running operation. Scopes allow multiple
/// components running within an operation to share the same context.  They are useful when different parts of an
/// operation may want to present a user with a distinct description with progress independent from other parts of the
/// long running operation.
/// </summary>
public interface ILongRunningOperationScope : IDisposable
{
    /// <summary>
    /// Gets user readable operation description for this scope.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Progress tracker instance to report operation progress.
    /// </summary>
    IProgress<LongRunningOperationProgress> Progress { get; }
}
