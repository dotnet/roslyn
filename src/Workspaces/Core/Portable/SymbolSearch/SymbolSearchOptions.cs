// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    [DataContract]
    internal readonly record struct SymbolSearchOptions(
        [property: DataMember(Order = 0)] bool SearchReferenceAssemblies = true,
        [property: DataMember(Order = 1)] bool SearchNuGetPackages = true)
    {
        public SymbolSearchOptions()
            : this(SearchReferenceAssemblies: true)
        {
        }

        public static readonly SymbolSearchOptions Default = new();
    }
}
