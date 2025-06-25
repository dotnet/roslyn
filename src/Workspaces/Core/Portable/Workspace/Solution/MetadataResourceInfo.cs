// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Describes a manifest resource specification stored in command line arguments.
/// </summary>
[DataContract]
internal sealed class MetadataResourceInfo(string resourceName, string filePath, string? linkedResourceFileName, bool isPublic, int contentVersion) : IEquatable<MetadataResourceInfo>
{
    /// <summary>
    /// Name of the manifest resource as it appears in metadata.
    /// Must be unique within project.
    /// </summary>
    [DataMember(Order = 0)]
    public string ResourceName { get; init; } = resourceName ?? throw new ArgumentNullException(nameof(resourceName));

    /// <summary>
    /// Full path to the resource content file.
    /// </summary>
    [DataMember(Order = 1)]
    public string FilePath { get; init; } = filePath ?? throw new ArgumentNullException(nameof(filePath));

    /// <summary>
    /// File name of a linked resource, or null if the resource is embedded.
    /// </summary>
    [DataMember(Order = 2)]
    public string? LinkedResourceFileName { get; init; } = linkedResourceFileName;

    /// <summary>
    /// Accessibility of the resource.
    /// </summary>
    [DataMember(Order = 3)]
    public bool IsPublic { get; init; } = isPublic;

    /// <summary>
    /// Version of the content. Used to determine if the content has changed since it was last written.
    /// </summary>
    [DataMember(Order = 4)]
    public int ContentVersion { get; init; } = contentVersion;

    public bool Equals(MetadataResourceInfo? other)
        => other != null
        && ResourceName == other.ResourceName
        && FilePath == other.FilePath
        && LinkedResourceFileName == other.LinkedResourceFileName
        && IsPublic == other.IsPublic
        && ContentVersion == other.ContentVersion;

    public override bool Equals(object? obj)
        => obj is MetadataResourceInfo info && Equals(info);

    public override int GetHashCode()
        => Hash.Combine(ResourceName, Hash.Combine(FilePath, Hash.Combine(LinkedResourceFileName, Hash.Combine(IsPublic, ContentVersion))));

    public override string ToString()
        => $"{ResourceName} (v{ContentVersion})";

    public MetadataResourceInfo WithContentVersion(int value)
        => value == ContentVersion ? this : new MetadataResourceInfo(ResourceName, FilePath, LinkedResourceFileName, IsPublic, value);

    public void WriteTo(ObjectWriter writer)
    {
        writer.WriteString(ResourceName);
        writer.WriteString(FilePath);
        writer.WriteString(LinkedResourceFileName);
        writer.WriteBoolean(IsPublic);
        writer.WriteInt32(ContentVersion);
    }

    public static MetadataResourceInfo ReadFrom(ObjectReader reader)
        => new(
            reader.ReadRequiredString(),
            reader.ReadRequiredString(),
            reader.ReadString(),
            reader.ReadBoolean(),
            reader.ReadInt32());

    /// <summary>
    /// True if the resource is embedded.
    /// </summary>
    public bool IsEmbedded
        => LinkedResourceFileName == null;

    /// <summary>
    /// True if the resource is linked.
    /// </summary>
    [MemberNotNullWhen(true, nameof(LinkedResourceFileName))]
    public bool IsLinked
        => LinkedResourceFileName != null;
}
