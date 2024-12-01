// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Classification;

[DataContract]
internal readonly record struct ClassificationOptions
{
    [DataMember] public bool ClassifyReassignedVariables { get; init; } = false;
    [DataMember] public bool ClassifyObsoleteSymbols { get; init; } = true;
    [DataMember] public bool ColorizeRegexPatterns { get; init; } = true;
    [DataMember] public bool ColorizeJsonPatterns { get; init; } = true;
    [DataMember] public bool FrozenPartialSemantics { get; init; } = false;

    public ClassificationOptions()
    {
    }

    public static readonly ClassificationOptions Default = new();
}
