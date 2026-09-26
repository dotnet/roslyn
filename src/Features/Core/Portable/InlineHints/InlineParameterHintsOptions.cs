// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.InlineHints;

[DataContract]
internal readonly record struct InlineParameterHintsOptions
{
    [DataMember] public bool EnabledForParameters { get; init; } = false;
    [DataMember] public bool ForLiteralParameters { get; init; } = true;
    [DataMember] public bool ForIndexerParameters { get; init; } = true;
    [DataMember] public bool ForObjectCreationParameters { get; init; } = true;
    [DataMember] public bool ForOtherParameters { get; init; } = false;
    [DataMember] public bool SuppressForParametersThatDifferOnlyBySuffix { get; init; } = true;
    [DataMember] public bool SuppressForParametersThatMatchMethodIntent { get; init; } = true;
    [DataMember] public bool SuppressForParametersThatMatchArgumentName { get; init; } = true;
    [DataMember] public bool SuppressForParametersThatMatchMemberName { get; init; } = true;

    public InlineParameterHintsOptions()
    {
    }

    public static readonly InlineParameterHintsOptions Default = new();
}
