// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class Request
{
    /// <summary>
    /// The request ID.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// The index of the targeted object.
    /// </summary>
    public int TargetObject { get; init; }

    /// <summary>
    /// The name of the method to call.
    /// </summary>
    public required string Method { get; init; }

    public required ImmutableArray<JsonElement> Parameters { get; init; }
}
