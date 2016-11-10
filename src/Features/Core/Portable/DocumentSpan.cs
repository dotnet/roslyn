// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a <see cref="TextSpan"/> location in a <see cref="Document"/>.
    /// </summary>
    internal struct DocumentSpan : IEquatable<DocumentSpan>
    {
        public Document Document { get; }
        public TextSpan SourceSpan { get; }

        public DocumentSpan(Document document, TextSpan sourceSpan)
        {
            Document = document;
            SourceSpan = sourceSpan;
        }

        public override bool Equals(object obj)
        {
            return Equals((DocumentSpan)obj);
        }

        public bool Equals(DocumentSpan obj)
        {
            return this.Document == obj.Document && this.SourceSpan == obj.SourceSpan;
        }

        public static bool operator ==(DocumentSpan d1, DocumentSpan d2)
        {
            return d1.Equals(d2);
        }

        public static bool operator !=(DocumentSpan d1, DocumentSpan d2)
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