// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    internal readonly struct ProjectOrDocumentId : IEquatable<ProjectOrDocumentId>
    {
        private readonly object? _projectOrDocumentId;

        public ProjectOrDocumentId(ProjectId projectId)
        {
            _projectOrDocumentId = projectId ?? throw new ArgumentNullException(nameof(projectId));
        }

        public ProjectOrDocumentId(DocumentId documentId)
        {
            _projectOrDocumentId = documentId ?? throw new ArgumentNullException(nameof(documentId));
        }

        public bool IsDefault => _projectOrDocumentId is null;

        public ProjectId ProjectId => DocumentId?.ProjectId ?? _projectOrDocumentId as ProjectId ?? throw new InvalidOperationException();

        public DocumentId? DocumentId => _projectOrDocumentId as DocumentId;

        public static implicit operator ProjectOrDocumentId(ProjectId projectId)
            => new ProjectOrDocumentId(projectId);

        public static implicit operator ProjectOrDocumentId(DocumentId documentId)
            => new ProjectOrDocumentId(documentId);

        public static bool operator ==(ProjectOrDocumentId left, ProjectOrDocumentId right)
            => left.Equals(right);

        public static bool operator !=(ProjectOrDocumentId left, ProjectOrDocumentId right)
            => !left.Equals(right);

        public void Deconstruct(out ProjectId projectId, out DocumentId? documentId)
            => (projectId, documentId) = (ProjectId, DocumentId);

        public override string ToString()
            => _projectOrDocumentId?.ToString() ?? string.Empty;

        public override bool Equals(object? obj)
            => obj is ProjectOrDocumentId other && Equals(other);

        public bool Equals(ProjectOrDocumentId other)
            => Equals(_projectOrDocumentId, other._projectOrDocumentId);

        public override int GetHashCode()
            => EqualityComparer<object?>.Default.GetHashCode(_projectOrDocumentId);
    }
}
