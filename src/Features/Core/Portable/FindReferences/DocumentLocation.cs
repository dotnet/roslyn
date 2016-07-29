// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindReferences
{
    /// <summary>
    /// Represents a <see cref="TextSpan"/> location in a <see cref="Document"/>.
    /// </summary>
    internal struct DocumentLocation : IEquatable<DocumentLocation>
    {
        public Document Document { get; }
        public TextSpan SourceSpan { get; }

        public DocumentLocation(Document document, TextSpan sourceSpan)
        {
            Document = document;
            SourceSpan = sourceSpan;
        }

        public override bool Equals(object obj)
        {
            return Equals((DocumentLocation)obj);
        }

        public bool Equals(DocumentLocation obj)
        {
            return this.Document == obj.Document && this.SourceSpan == obj.SourceSpan;
        }

        public static bool operator ==(DocumentLocation d1, DocumentLocation d2)
        {
            return d1.Equals(d2);
        }

        public static bool operator !=(DocumentLocation d1, DocumentLocation d2)
        {
            return !(d1 == d2);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                this.Document,
                this.SourceSpan.GetHashCode());
        }

        public bool CanNavigateTo()
        {
            var workspace = Document.Project.Solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.CanNavigateToPosition(workspace, Document.Id, SourceSpan.Start);
        }

        public bool TryNavigateTo()
        {
            var workspace = Document.Project.Solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.TryNavigateToPosition(workspace, Document.Id, SourceSpan.Start);
        }
    }
}