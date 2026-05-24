// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Diagnostics;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.CodeAnalysis.Remote.Razor.Diagnostics;

[Export(typeof(RazorTranslateDiagnosticsService)), Shared]
[method: ImportingConstructor]
internal sealed class RemoteRazorTranslateDiagnosticsService(
    IDocumentMappingService documentMappingService,
    ILoggerFactory loggerFactory) : RazorTranslateDiagnosticsService(documentMappingService, loggerFactory)
{
}
