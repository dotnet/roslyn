// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PdbSourceDocument;

namespace Microsoft.CodeAnalysis.LanguageServer.Services.SourceLink;

[ExportWorkspaceServiceFactory(typeof(ISourceLinkService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSCodeSourceLinkServiceFactory(
    IServiceBrokerProvider serviceBrokerProvider,
    [Import(AllowDefault = true)] IPdbSourceDocumentLogger? logger) : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new VSCodeSourceLinkService(serviceBrokerProvider.ServiceBroker, logger);
}
