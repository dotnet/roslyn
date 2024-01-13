// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.SymbolSearch;

[DataContract]
internal readonly record struct SymbolSearchOptions
{
    [DataMember] public bool SearchReferenceAssemblies { get; init; } = true;
    [DataMember] public bool SearchNuGetPackages { get; init; } = true;

    // required to make sure new SymbolSearchOptions() runs property initializers
    public SymbolSearchOptions()
    {
    }

    public static readonly SymbolSearchOptions Default = new();
}
