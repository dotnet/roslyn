// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol.DocumentSymbols;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentSymbols;

[Export(typeof(IDocumentSymbolService)), Shared]
[method: ImportingConstructor]
internal class OOPDocumentSymbolService(IDocumentMappingService documentMappingService) : DocumentSymbolService(documentMappingService)
{
}
