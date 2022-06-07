﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// An identifier that can be used to refer to the same <see cref="Project"/> across versions.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public sealed class ProjectId : IEquatable<ProjectId>, IObjectWritable
    {
        private readonly string? _debugName;

        /// <summary>
        /// The system generated unique id.
        /// </summary>
        public Guid Id { get; }

        private ProjectId(Guid guid, string? debugName)
        {
            this.Id = guid;
            _debugName = debugName;
        }

        /// <summary>
        /// Create a new ProjectId instance.
        /// </summary>
        /// <param name="debugName">An optional name to make this id easier to recognize while debugging.</param>
        public static ProjectId CreateNewId(string? debugName = null)
            => new(Guid.NewGuid(), debugName);

        public static ProjectId CreateFromSerialized(Guid id, string? debugName = null)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            return new ProjectId(id, debugName);
        }

        internal string? DebugName => _debugName;

        private string GetDebuggerDisplay()
            => string.Format("({0}, #{1} - {2})", this.GetType().Name, this.Id, _debugName);

        public override string ToString()
            => GetDebuggerDisplay();

        public override bool Equals(object? obj)
            => this.Equals(obj as ProjectId);

        public bool Equals(ProjectId? other)
        {
            return
                other is object &&
                this.Id == other.Id;
        }

        public static bool operator ==(ProjectId? left, ProjectId? right)
            => EqualityComparer<ProjectId?>.Default.Equals(left, right);

        public static bool operator !=(ProjectId? left, ProjectId? right)
            => !(left == right);

        public override int GetHashCode()
            => this.Id.GetHashCode();

        bool IObjectWritable.ShouldReuseInSerialization => true;

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            writer.WriteGuid(Id);
            writer.WriteString(DebugName);
        }

        internal static ProjectId ReadFrom(ObjectReader reader)
        {
            var guid = reader.ReadGuid();
            var debugName = reader.ReadString();

            return CreateFromSerialized(guid, debugName);
        }
    }
}
