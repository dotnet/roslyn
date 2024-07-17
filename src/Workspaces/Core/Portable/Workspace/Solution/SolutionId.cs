// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// An identifier that can be used to refer to the same Solution across versions. 
/// </summary>
[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
[DataContract]
public sealed class SolutionId : IEquatable<SolutionId>
{
    /// <summary>
    /// The unique id of the solution.
    /// </summary>
    [DataMember(Order = 0)]
    public Guid Id { get; }

    [DataMember(Order = 1)]
    private readonly string _debugName;

    private SolutionId(Guid id, string debugName)
    {
        this.Id = id;
        _debugName = debugName;
    }

    /// <summary>
    /// Create a new Solution Id
    /// </summary>
    /// <param name="debugName">An optional name to make this id easier to recognize while debugging.</param>
    public static SolutionId CreateNewId(string debugName = null)
        => CreateFromSerialized(Guid.NewGuid(), debugName);

    public static SolutionId CreateFromSerialized(Guid id, string debugName = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(nameof(id));
        }

        debugName ??= "unsaved";

        return new SolutionId(id, debugName);
    }

    internal string DebugName => _debugName;

    private string GetDebuggerDisplay()
        => string.Format("({0}, #{1} - {2})", GetType().Name, this.Id, _debugName);

    public override bool Equals(object obj)
        => this.Equals(obj as SolutionId);

    public bool Equals(SolutionId other)
    {
        return
            other is object &&
            this.Id == other.Id;
    }

    public static bool operator ==(SolutionId left, SolutionId right)
        => EqualityComparer<SolutionId>.Default.Equals(left, right);

    public static bool operator !=(SolutionId left, SolutionId right)
        => !(left == right);

    public override int GetHashCode()
        => this.Id.GetHashCode();

    internal void WriteTo(ObjectWriter writer)
    {
        writer.WriteGuid(Id);
        writer.WriteString(DebugName);
    }

    internal static SolutionId ReadFrom(ObjectReader reader)
    {
        var guid = reader.ReadGuid();
        var debugName = reader.ReadString();

        return CreateFromSerialized(guid, debugName);
    }
}
