// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
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

        /// <summary>
        /// Additional information attached to a document span by it creator.
        /// </summary>
        public ImmutableDictionary<string, object> Properties { get; }

        public DocumentSpan(Document document, TextSpan sourceSpan)
            : this(document, sourceSpan, properties: null)
        {
        }

        public DocumentSpan(
            Document document,
            TextSpan sourceSpan,
            ImmutableDictionary<string, object> properties)
        {
            Document = document;
            SourceSpan = sourceSpan;
            Properties = properties ?? ImmutableDictionary<string, object>.Empty;
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
    }

    internal static class DocumentSpanExtensions
    {
        public static bool CanNavigateTo(this DocumentSpan documentSpan)
        {
            var workspace = documentSpan.Document.Project.Solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.CanNavigateToSpan(workspace, documentSpan.Document.Id, documentSpan.SourceSpan);
        }

        public static bool TryNavigateTo(this DocumentSpan documentSpan, bool isPreview)
        {
            var solution = documentSpan.Document.Project.Solution;
            var workspace = solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.TryNavigateToSpan(workspace, documentSpan.Document.Id, documentSpan.SourceSpan,
                options: solution.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, isPreview));
        }

        public static async Task<bool> IsHiddenAsync(
            this DocumentSpan documentSpan, CancellationToken cancellationToken)
        {
            var document = documentSpan.Document;
            if (document.SupportsSyntaxTree)
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                return tree.IsHiddenPosition(documentSpan.SourceSpan.Start, cancellationToken);
            }

            return false;
        }
    }
}
