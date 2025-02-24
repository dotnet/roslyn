// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MethodImplementation;

internal sealed record MethodImplementationParameterContext
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required ImmutableArray<string> Modifiers { get; init; }
}
