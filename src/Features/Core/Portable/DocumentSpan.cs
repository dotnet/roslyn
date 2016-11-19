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
            => Equals((DocumentSpan)obj);

        public bool Equals(DocumentSpan obj)
            => this.Document == obj.Document && this.SourceSpan == obj.SourceSpan;

        public static bool operator ==(DocumentSpan d1, DocumentSpan d2)
            => d1.Equals(d2);

        public static bool operator !=(DocumentSpan d1, DocumentSpan d2)
            => !(d1 == d2);

        public override int GetHashCode()
            => Hash.Combine(
                this.Document,
                this.SourceSpan.GetHashCode());

        public bool CanNavigateTo()
        {
            var workspace = Document.Project.Solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.CanNavigateToSpan(workspace, Document.Id, SourceSpan);
        }

        public bool TryNavigateTo()
        {
            var solution = Document.Project.Solution;
            var workspace = solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.TryNavigateToSpan(workspace, Document.Id, SourceSpan,
                options: solution.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, true));
        }
    }
}