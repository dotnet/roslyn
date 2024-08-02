// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editing;

/// <summary>
/// An editor for making changes to a document's syntax tree. 
/// </summary>
public class DocumentEditor : SyntaxEditor
{
    private readonly SemanticModel _model;

    private DocumentEditor(Document document, SemanticModel model, SyntaxNode root)
        : base(root, document.Project.Solution.Services)
    {
        OriginalDocument = document;
        _model = model;
    }

    /// <summary>
    /// Creates a new <see cref="DocumentEditor"/> instance.
    /// </summary>
    public static async Task<DocumentEditor> CreateAsync(Document document, CancellationToken cancellationToken = default)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = model.SyntaxTree.GetRoot(cancellationToken);
        return new DocumentEditor(document, model, root);
    }

    /// <summary>
    /// The <see cref="Document"/> specified when the editor was first created.
    /// </summary>
    public Document OriginalDocument { get; }

    /// <summary>
    /// The <see cref="CodeAnalysis.SemanticModel"/> of the original document.
    /// </summary>
    public SemanticModel SemanticModel => _model;

    /// <summary>
    /// Returns the changed <see cref="Document"/>.
    /// </summary>
    public Document GetChangedDocument()
        => OriginalDocument.WithSyntaxRoot(this.GetChangedRoot());
}
