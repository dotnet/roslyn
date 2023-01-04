// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.InlineHints;

[DataContract]
internal readonly record struct InlineHintsOptions
{
    [DataMember] public InlineParameterHintsOptions ParameterOptions { get; init; } = InlineParameterHintsOptions.Default;
    [DataMember] public InlineTypeHintsOptions TypeOptions { get; init; } = InlineTypeHintsOptions.Default;
    [DataMember] public SymbolDescriptionOptions DisplayOptions { get; init; } = SymbolDescriptionOptions.Default;

    public InlineHintsOptions()
    {
    }

    public static readonly InlineHintsOptions Default = new();
}
