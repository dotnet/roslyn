﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

internal static class ITextBufferExtensions
{
    public static bool TryGetTextDocument(this ITextBuffer textBuffer, [NotNullWhen(true)] out TextDocument? textDocument)
    {
        textDocument = textBuffer.CurrentSnapshot.AsText().GetOpenTextDocumentInCurrentContextWithChanges();
        return textDocument is not null;
    }
}
