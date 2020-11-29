// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal interface ILspSolutionProvider
    {
        /// <summary>
        /// Finds the document, and the solution it comes from, in any workspace that has registered for LSP
        /// and including only documents where the client name matches.
        /// </summary>
        (DocumentId?, Solution) FindDocumentAndSolution(TextDocumentIdentifier? textDocument, string? clientName);
    }
}
