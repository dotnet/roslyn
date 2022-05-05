// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;
/// <summary>
/// Associates LSP document URIs with the roslyn source text containing the LSP document text.
/// Called via <see cref="DidOpenHandler"/>, <see cref="DidChangeHandler"/> and <see cref="DidCloseHandler"/>
/// </summary>
internal interface IDocumentChangeTracker
{
    void StartTracking(Uri documentUri, SourceText initialText);
    void UpdateTrackedDocument(Uri documentUri, SourceText text);
    void StopTracking(Uri documentUri);
}

internal class NonMutatingDocumentChangeTracker : IDocumentChangeTracker
{
    public void StartTracking(Uri documentUri, SourceText initialText)
    {
        throw new InvalidOperationException("Mutating documents not allowed in a non-mutating request handler");
    }

    public void StopTracking(Uri documentUri)
    {
        throw new InvalidOperationException("Mutating documents not allowed in a non-mutating request handler");
    }

    public void UpdateTrackedDocument(Uri documentUri, SourceText text)
    {
        throw new InvalidOperationException("Mutating documents not allowed in a non-mutating request handler");
    }
}
