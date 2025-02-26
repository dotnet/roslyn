// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.MethodImplementation;

internal sealed record ReferenceContext
{
    public required string FileName { get; init; }
    public required string SurroundingCode { get; init; }
}
