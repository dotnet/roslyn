// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.QuickInfo;

[DataContract]
internal readonly record struct QuickInfoOptions
{
    [DataMember] public bool ShowRemarksInQuickInfo { get; init; } = true;
    [DataMember] public bool IncludeNavigationHintsInQuickInfo { get; init; } = true;

    public QuickInfoOptions()
    {
    }

    public static readonly QuickInfoOptions Default = new();
}
