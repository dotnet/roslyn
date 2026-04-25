// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Remote.Razor.Formatting;

[Export(typeof(IRazorFormattingService)), Shared]
[method: ImportingConstructor]
internal sealed class RemoteRazorFormattingService(IDocumentMappingService documentMappingService, IRazorEditService razorEditService, IHostServicesProvider hostServicesProvider, IFormattingLoggerFactory formattingLoggerFactory, ILoggerFactory loggerFactory)
    : RazorFormattingService(documentMappingService, razorEditService, hostServicesProvider, formattingLoggerFactory, loggerFactory)
{
}
