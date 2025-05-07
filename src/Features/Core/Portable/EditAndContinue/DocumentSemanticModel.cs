// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal readonly struct DocumentSemanticModel
{
    /// <summary>
    /// <see cref="SemanticModel"/> for the document, or null if the document has been deleted.
    /// The <see cref="SyntaxTree"/> is empty in the latter case.
    /// </summary>
    public readonly SemanticModel? Model;

    public readonly Compilation Compilation;
    public readonly SyntaxTree SyntaxTree;

    public DocumentSemanticModel(SemanticModel model)
    {
        Model = model;
        Compilation = model.Compilation;
        SyntaxTree = model.SyntaxTree;
    }

    public DocumentSemanticModel(Compilation compilation, SyntaxTree syntaxTree)
    {
        Debug.Assert(syntaxTree.GetText().Length == 0);

        Compilation = compilation;
        SyntaxTree = syntaxTree;
    }

    /// <summary>
    /// Semnatic model can only be used if we have a syntax node from the document (the tree is not empty).
    /// </summary>
    public SemanticModel RequiredModel
    {
        get
        {
            Contract.ThrowIfNull(Model);
            return Model;
        }
    }
}
