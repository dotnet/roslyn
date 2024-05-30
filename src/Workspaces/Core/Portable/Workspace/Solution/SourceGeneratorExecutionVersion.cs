// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents the version of source generator execution that a project is at. Source generator results are kept around
/// as long as this version stays the same and we are in <see cref="SourceGeneratorExecutionPreference.Manual"/>
/// mode. This has no effect when in <see cref="SourceGeneratorExecutionPreference.Automatic"/> mode (as we always rerun
/// generators on any change). This should effectively be used as a monotonically increasing value.
/// </summary>
/// <param name="MajorVersion">Controls the major version of source generation execution.  When this changes the
/// generator driver should be dropped and all generation should be rerun.</param>
/// <param name="MinorVersion">Controls the minor version of source generation execution.  When this changes the
/// generator driver can be reused and should incrementally determine what the new generated documents should be.
/// </param>
[DataContract]
internal readonly record struct SourceGeneratorExecutionVersion(
    [property: DataMember(Order = 0)] int MajorVersion,
    [property: DataMember(Order = 1)] int MinorVersion)
{
    public SourceGeneratorExecutionVersion IncrementMajorVersion()
        => new(MajorVersion + 1, MinorVersion: 0);

    public SourceGeneratorExecutionVersion IncrementMinorVersion()
        => new(MajorVersion, MinorVersion + 1);

    public void WriteTo(ObjectWriter writer)
    {
        writer.WriteInt32(MajorVersion);
        writer.WriteInt32(MinorVersion);
    }

    public static SourceGeneratorExecutionVersion ReadFrom(ObjectReader reader)
        => new(reader.ReadInt32(), reader.ReadInt32());

    public override string ToString()
        => $"{MajorVersion}.{MinorVersion}";
}

/// <summary>
/// Helper construct to allow a mapping from <see cref="ProjectId"/>s to <see cref="SourceGeneratorExecutionVersion"/>.
/// Limited to just the surface area the workspace needs.
/// </summary>
internal sealed class SourceGeneratorExecutionVersionMap(ImmutableSortedDictionary<ProjectId, SourceGeneratorExecutionVersion> map)
{
    public static readonly SourceGeneratorExecutionVersionMap Empty = new();

    public ImmutableSortedDictionary<ProjectId, SourceGeneratorExecutionVersion> Map { get; } = map;

    public SourceGeneratorExecutionVersionMap()
        : this(ImmutableSortedDictionary<ProjectId, SourceGeneratorExecutionVersion>.Empty)
    {
    }

    public SourceGeneratorExecutionVersion this[ProjectId projectId] => Map[projectId];

    public static bool operator ==(SourceGeneratorExecutionVersionMap map1, SourceGeneratorExecutionVersionMap map2)
        => map1.Map == map2.Map;

    public static bool operator !=(SourceGeneratorExecutionVersionMap map1, SourceGeneratorExecutionVersionMap map2)
        => !(map1 == map2);

    public override int GetHashCode()
        => throw new InvalidOperationException();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is SourceGeneratorExecutionVersionMap map && this == map;

    public void WriteTo(ObjectWriter writer)
    {
        // Writing out the dictionary in order is fine.  That's because it's a sorted dictionary, and ProjectIds are
        // naturally comparable.
        writer.WriteInt32(Map.Count);
        foreach (var (projectId, version) in Map)
        {
            projectId.WriteTo(writer);
            version.WriteTo(writer);
        }
    }

    public static SourceGeneratorExecutionVersionMap Deserialize(ObjectReader reader)
    {
        var count = reader.ReadInt32();
        var builder = ImmutableSortedDictionary.CreateBuilder<ProjectId, SourceGeneratorExecutionVersion>();
        for (var i = 0; i < count; i++)
        {
            var projectId = ProjectId.ReadFrom(reader);
            var version = SourceGeneratorExecutionVersion.ReadFrom(reader);
            builder.Add(projectId, version);
        }

        return new(builder.ToImmutable());
    }

    public Checksum GetChecksum()
    {
        using var _ = ArrayBuilder<Checksum>.GetInstance(this.Map.Count * 2, out var checksums);

        foreach (var (projectId, version) in this.Map)
        {
            checksums.Add(projectId.Checksum);
            checksums.Add(Checksum.Create(version, static (v, w) => v.WriteTo(w)));
        }

        return Checksum.Create(checksums);
    }

    public override string ToString()
    {
        using var _ = PooledStringBuilder.GetInstance(out var builder);

        builder.AppendLine(nameof(SourceGeneratorExecutionVersionMap));
        foreach (var (projectId, version) in Map)
            builder.AppendLine($"    {projectId}: {version}");

        return builder.ToString();
    }
}
