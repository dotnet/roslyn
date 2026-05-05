// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

internal static class ITextBufferExtensions
{
    public static bool TryGetTextDocument(this ITextBuffer textBuffer, [NotNullWhen(true)] out TextDocument? textDocument)
    {
        textDocument = textBuffer.CurrentSnapshot.AsText().GetOpenTextDocumentInCurrentContextWithChanges();
        return textDocument is not null;
    }
}
