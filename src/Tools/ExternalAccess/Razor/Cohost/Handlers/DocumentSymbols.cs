// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class DocumentSymbols
{
    public static Task<SumType<DocumentSymbol[], SymbolInformation[]>> GetDocumentSymbolsAsync(
        Document document, bool useHierarchicalSymbols, bool supportsVSExtensions, CancellationToken cancellationToken)
    {
        // supportsVSExtensions controls whether or not any SymbolInformation that's returned is a VSSymbolInformation
        // with a VSImageId. This value should be retrieved from the language server's client capabilities.
        return DocumentSymbolsHandler.GetDocumentSymbolsAsync(document, useHierarchicalSymbols, supportsVSExtensions, cancellationToken);
    }

    [Obsolete("Update to call overload that takes 'supportsVSExtensions' argument.")]
    public static Task<SumType<DocumentSymbol[], SymbolInformation[]>> GetDocumentSymbolsAsync(
        Document document, bool useHierarchicalSymbols, CancellationToken cancellationToken) 
        => GetDocumentSymbolsAsync(document, useHierarchicalSymbols, supportsVSExtensions: false, cancellationToken);
}
