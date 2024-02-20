// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Remote.ProjectSystem;

[DataContract]
internal readonly record struct MetadataReferenceInfo(
    [property: DataMember(Order = 0)] string FilePath,
    [property: DataMember(Order = 1)] string? Aliases,
    [property: DataMember(Order = 2)] bool EmbedInteropTypes)
{
    public MetadataReferenceProperties CreateProperties()
    {
        return new MetadataReferenceProperties(aliases: Aliases != null ? [Aliases] : default, embedInteropTypes: EmbedInteropTypes);
    }
}
