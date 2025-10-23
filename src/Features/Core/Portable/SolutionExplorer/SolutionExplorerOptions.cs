// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.SolutionExplorer;

[DataContract]
internal readonly record struct SolutionExplorerOptions
{
    [DataMember] public bool ShowLanguageSymbolsInsideSolutionExplorerFiles { get; init; } = true;

    public SolutionExplorerOptions()
    {
    }

    public static readonly SolutionExplorerOptions Default = new();
}
