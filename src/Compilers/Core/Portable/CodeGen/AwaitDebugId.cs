// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeGen;

/// <summary>
/// Identifies a specific await within a set of awaits generated for a syntax node. 
/// 
/// If multiple await expressions are produced for the same syntax node EnC needs to know how they map to specific async calls.
/// For example, `await foreach` generates two awaits -- one for MoveNextAsync (<paramref name="RelativeStateOrdinal"/> is 0)
/// and the other for DisposeAsync (<paramref name="RelativeStateOrdinal"/> is 1).
/// </summary>
internal readonly record struct AwaitDebugId(byte RelativeStateOrdinal);
