// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.MethodImplementation;

[DataContract]
internal readonly record struct MethodImplementationOptions()
{
    public static readonly MethodImplementationOptions Default = new();

    [DataMember] public int ContextLineCount { get; init; } = 10;
    [DataMember] public int MaxContainingTypeLength { get; init; } = 200;
    [DataMember] public int MaxMethodLength { get; init; } = 1024;
    [DataMember] public int MaxSurroundingCodeSnippets { get; init; } = 2;
}
