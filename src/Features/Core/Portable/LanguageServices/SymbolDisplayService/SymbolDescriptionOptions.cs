// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.LanguageService;

[DataContract]
internal readonly record struct SymbolDescriptionOptions
{
    [DataMember] public QuickInfoOptions QuickInfoOptions { get; init; } = QuickInfoOptions.Default;
    [DataMember] public ClassificationOptions ClassificationOptions { get; init; } = ClassificationOptions.Default;

    public SymbolDescriptionOptions()
    {
    }

    public static readonly SymbolDescriptionOptions Default = new();
}
