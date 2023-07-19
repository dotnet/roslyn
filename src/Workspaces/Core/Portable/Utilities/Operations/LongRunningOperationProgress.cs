// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents an update of a progress.
/// </summary>
/// <param name="CompletedItems">A number of already completed items.</param>
/// <param name="TotalItems">A total number of items.</param>
public readonly record struct LongRunningOperationProgress(int CompletedItems, int TotalItems);
