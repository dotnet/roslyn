// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Extensions;

/// <summary>
/// Represents the context of a document extension message handler.
/// </summary>
[Experimental("RSEXPERIMENTAL008", UrlFormat = "https://github.com/dotnet/roslyn/pull/85209")]
public readonly struct ExtensionDocumentMessageContext
{
    internal ExtensionDocumentMessageContext(TextDocument textDocument)
    {
        TextDocument = textDocument;
        Solution = textDocument.Project.Solution;
    }

    /// <summary>
    /// Gets the current solution state.
    /// </summary>
    public Solution Solution { get; }

    /// <summary>
    /// Gets the text document the message refers to.
    /// </summary>
    public TextDocument TextDocument { get; }
}
