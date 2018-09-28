// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// An identifier that can be used to retrieve the same <see cref="Document"/> across versions of the
    /// workspace.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public sealed class DocumentId : IEquatable<DocumentId>, IObjectWritable
    {
        public ProjectId ProjectId { get; }
        public Guid Id { get; }

        private readonly string _debugName;

        private DocumentId(ProjectId projectId, Guid guid, string debugName)
        {
            this.ProjectId = projectId;
            this.Id = guid;
            _debugName = debugName;
        }

        /// <summary>
        /// Creates a new <see cref="DocumentId"/> instance.
        /// </summary>
        /// <param name="projectId">The project id this document id is relative to.</param>
        /// <param name="debugName">An optional name to make this id easier to recognize while debugging.</param>
        public static DocumentId CreateNewId(ProjectId projectId, string debugName = null)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            return new DocumentId(projectId, Guid.NewGuid(), debugName);
        }

        public static DocumentId CreateFromSerialized(ProjectId projectId, Guid id, string debugName = null)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            return new DocumentId(projectId, id, debugName);
        }

        internal string DebugName => _debugName;

        internal string GetDebuggerDisplay()
        {
            return string.Format("({0}, #{1} - {2})", this.GetType().Name, this.Id, _debugName);
        }

        public override string ToString()
        {
            return GetDebuggerDisplay();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as DocumentId);
        }

        public bool Equals(DocumentId other)
        {
            // Technically, we don't need to check project id.
            return
                !ReferenceEquals(other, null) &&
                this.Id == other.Id &&
                this.ProjectId == other.ProjectId;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.ProjectId, this.Id.GetHashCode());
        }

        public static bool operator ==(DocumentId left, DocumentId right)
        {
            return EqualityComparer<DocumentId>.Default.Equals(left, right);
        }

        public static bool operator !=(DocumentId left, DocumentId right)
        {
            return !(left == right);
        }

        bool IObjectWritable.ShouldReuseInSerialization => true;

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            ProjectId.WriteTo(writer);

            writer.WriteGuid(Id);
            writer.WriteString(DebugName);
        }

        internal static DocumentId ReadFrom(ObjectReader reader)
        {
            var projectId = ProjectId.ReadFrom(reader);

            var guid = reader.ReadGuid();
            var debugName = reader.ReadString();

            return CreateFromSerialized(projectId, guid, debugName);
        }
    }
}
