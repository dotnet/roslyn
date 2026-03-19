// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.Snippets;

/// <summary>
/// The context presented to a <see cref="ISnippetProvider"/> when providing completions.
/// </summary>
internal readonly struct SnippetContext
{
    internal SnippetContext(SyntaxContext syntaxContext)
    {
        SyntaxContext = syntaxContext ?? throw new ArgumentNullException(nameof(syntaxContext));
    }

    /// <summary>
    /// The document that the snippet was invoked within.
    /// </summary>
    public Document Document => SyntaxContext.Document;

    /// <summary>
    /// The caret position when the snippet was triggered.
    /// </summary>
    public int Position => SyntaxContext.Position;

    /// <summary>
    /// The semantic model of the document.
    /// </summary>
    public SemanticModel SemanticModel => SyntaxContext.SemanticModel;

    internal SyntaxContext SyntaxContext { get; }
}
