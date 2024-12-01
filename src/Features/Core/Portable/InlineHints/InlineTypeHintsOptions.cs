// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.InlineHints;

[DataContract]
internal readonly record struct InlineTypeHintsOptions
{
    [DataMember] public bool EnabledForTypes { get; init; } = false;
    [DataMember] public bool ForImplicitVariableTypes { get; init; } = true;
    [DataMember] public bool ForLambdaParameterTypes { get; init; } = true;
    [DataMember] public bool ForImplicitObjectCreation { get; init; } = true;
    [DataMember] public bool ForCollectionExpressions { get; init; } = true;

    public InlineTypeHintsOptions()
    {
    }

    public static readonly InlineTypeHintsOptions Default = new();
}
