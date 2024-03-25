// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// An identifier that can be used to retrieve the same <see cref="Document"/> across versions of the
/// workspace.
/// </summary>
[DataContract]
[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
public sealed class DocumentId : IEquatable<DocumentId>
{
    [DataMember(Order = 0)]
    public ProjectId ProjectId { get; }
    [DataMember(Order = 1)]
    public Guid Id { get; }
    [DataMember(Order = 2)]
    internal bool IsSourceGenerated { get; }
    [DataMember(Order = 3)]
    private readonly string? _debugName;

    private DocumentId(ProjectId projectId, Guid guid, bool isSourceGenerated, string? debugName)
    {
        this.ProjectId = projectId;
        this.Id = guid;
        this.IsSourceGenerated = isSourceGenerated;
        _debugName = debugName;
    }

    /// <summary>
    /// Creates a new <see cref="DocumentId"/> instance.
    /// </summary>
    /// <param name="projectId">The project id this document id is relative to.</param>
    /// <param name="debugName">An optional name to make this id easier to recognize while debugging.</param>
    public static DocumentId CreateNewId(ProjectId projectId, string? debugName = null)
        => CreateFromSerialized(projectId, Guid.NewGuid(), isSourceGenerated: false, debugName);

    public static DocumentId CreateFromSerialized(ProjectId projectId, Guid id, string? debugName = null)
        => CreateFromSerialized(projectId, id, isSourceGenerated: false, debugName);

    internal static DocumentId CreateFromSerialized(ProjectId projectId, Guid id, bool isSourceGenerated, string? debugName)
    {
        if (projectId == null)
            throw new ArgumentNullException(nameof(projectId));

        if (id == Guid.Empty)
            throw new ArgumentException(nameof(id));

        return new DocumentId(projectId, id, isSourceGenerated, debugName);
    }

    internal string? DebugName => _debugName;

    internal string GetDebuggerDisplay()
        => string.Format("({0}, #{1} - {2})", this.GetType().Name, this.Id, _debugName);

    public override string ToString()
        => GetDebuggerDisplay();

    public override bool Equals(object? obj)
        => this.Equals(obj as DocumentId);

    public bool Equals(DocumentId? other)
    {
        // Technically, we don't need to check project id.
        return
            other is not null &&
            this.Id == other.Id &&
            this.ProjectId == other.ProjectId;
    }

    public override int GetHashCode()
        => Hash.Combine(this.ProjectId, this.Id.GetHashCode());

    public static bool operator ==(DocumentId? left, DocumentId? right)
        => EqualityComparer<DocumentId?>.Default.Equals(left, right);

    public static bool operator !=(DocumentId? left, DocumentId? right)
        => !(left == right);

    internal void WriteTo(ObjectWriter writer)
    {
        this.ProjectId.WriteTo(writer);
        writer.WriteGuid(Id);
        writer.WriteBoolean(IsSourceGenerated);
        writer.WriteString(DebugName);
    }

    internal static DocumentId ReadFrom(ObjectReader reader)
    {
        var projectId = ProjectId.ReadFrom(reader);
        var guid = reader.ReadGuid();
        var isSourceGenerated = reader.ReadBoolean();
        var debugName = reader.ReadString();

        return CreateFromSerialized(projectId, guid, isSourceGenerated, debugName);
    }
}
